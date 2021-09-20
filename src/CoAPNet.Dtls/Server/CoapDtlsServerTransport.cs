using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CoAPNet.Dtls.Server.Statistics;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;

namespace CoAPNet.Dtls.Server
{
    internal class CoapDtlsServerTransport : ICoapTransport
    {
        private const int NetworkMtu = 1500;
        private static readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(15);

        private readonly CoapDtlsServerEndPoint _endPoint;
        private readonly ICoapHandler _coapHandler;
        private readonly IDtlsServerFactory _tlsServerFactory;
        private readonly ILogger<CoapDtlsServerTransport> _logger;
        private readonly DtlsServerProtocol _serverProtocol;
        private readonly ConcurrentDictionary<IPEndPoint, CoapDtlsServerClientEndPoint> _sessions;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private UdpClient _socket;
        private readonly BlockingCollection<UdpSendPacket> _sendQueue = new BlockingCollection<UdpSendPacket>();

        public CoapDtlsServerTransport(CoapDtlsServerEndPoint endPoint, ICoapHandler coapHandler, IDtlsServerFactory tlsServerFactory, ILogger<CoapDtlsServerTransport> logger)
        {
            _endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            _coapHandler = coapHandler ?? throw new ArgumentNullException(nameof(coapHandler));
            _tlsServerFactory = tlsServerFactory ?? throw new ArgumentNullException(nameof(tlsServerFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SecureRandom random = new SecureRandom();

            _serverProtocol = new DtlsServerProtocol(random);

            _sessions = new ConcurrentDictionary<IPEndPoint, CoapDtlsServerClientEndPoint>();
        }

        internal DtlsStatistics GetStatistics()
        {
            return new DtlsStatistics
            {
                Sessions = _sessions.Values.Select(x => new DtlsSessionStatistics
                {
                    EndPoint = x.EndPoint.ToString(),
                    ConnectionInfo = x.ConnectionInfo,
                    LastReceivedTime = x.LastReceivedTime,
                    SessionStartTime = x.SessionStartTime
                }).ToList()
            };
        }

        public Task BindAsync()
        {
            if (_socket != null)
                throw new InvalidOperationException("transport has already been used");

            _socket = new UdpClient(AddressFamily.InterNetworkV6);
            _socket.Client.DualMode = true;
            _socket.Client.Bind(_endPoint.IPEndPoint);

            _logger.LogInformation("Bound to Endpoint {IPEndpoint}", _endPoint.IPEndPoint);

            Task.Factory.StartNew(HandleIncoming, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(HandleOutgoing, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(HandleCleanup, TaskCreationOptions.LongRunning);

            return Task.CompletedTask;
        }

        private async Task HandleIncoming()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult data;
                    try
                    {
                        data = await _socket.ReceiveAsync();
                        _logger.LogDebug("Received DTLS Packet from {EndPoint}", data.RemoteEndPoint);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Happens when the Connection is being closed
                        continue;
                    }
                    catch (SocketException sockEx)
                    {
                        // Some clients send an ICMP Port Unreachable when they receive data after they stopped listening.
                        // If we knew from which client it came we could get rid of the connection, but we can't, so we just ignore the exception.
                        if (sockEx.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            _logger.LogInformation("Connection Closed by remote host");
                            continue;
                        }
                        _logger.LogError("SocketException with SocketErrorCode {SocketErrorCode}", sockEx.SocketErrorCode);
                        throw;
                    }

                    // if there is an existing session, we pass the datagram to the session.
                    if (_sessions.TryGetValue(data.RemoteEndPoint, out CoapDtlsServerClientEndPoint session))
                    {
                        session.EnqueueDatagram(data.Buffer);
                        continue;
                    }

                    // if there isn't an existing session for this remote endpoint, we start a new one and pass the first datagram to the session
                    var transport = new QueueDatagramTransport(NetworkMtu, bytes => _sendQueue.Add(new UdpSendPacket(bytes, data.RemoteEndPoint)));
                    session = new CoapDtlsServerClientEndPoint(data.RemoteEndPoint, transport, DateTime.UtcNow);
                    session.EnqueueDatagram(data.Buffer);

                    _sessions.TryAdd(data.RemoteEndPoint, session);
                    try
                    {
                        _logger.LogInformation("New connection from {EndPoint}; Active Sessions: {ActiveSessions}", data.RemoteEndPoint, _sessions.Count);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        Task.Factory.StartNew(() => HandleSession(session), TaskCreationOptions.LongRunning);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception while starting session handler!");
                        _sessions.TryRemove(session.EndPoint, out _);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while handling incoming datagrams");
                }
            }
        }

        private async Task HandleOutgoing()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    _sendQueue.TryTake(out UdpSendPacket toSend, Timeout.Infinite, _cts.Token);

                    await _socket.SendAsync(toSend.Payload, toSend.Payload.Length, toSend.TargetEndPoint);
                    _logger.LogDebug("Sent DTLS Packet to {EndPoint}", toSend.TargetEndPoint);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogDebug(ex, "Operation canceled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while handling outgoing datagrams");
                }
            }
        }

        private async Task HandleSession(CoapDtlsServerClientEndPoint session)
        {
            var state = new Dictionary<string, object>
                {
                    { "RemoteEndPoint", session?.EndPoint }
                };
            using (_logger.BeginScope(state))
            {
                try
                {
                    _logger.LogDebug("Trying to accept TLS connection from {EndPoint}", session.EndPoint);

                    var server = _tlsServerFactory.Create();

                    session.Accept(_serverProtocol, server);

                    if (session.ConnectionInfo != null)
                    {
                        _logger.LogInformation("New TLS connection from {EndPoint}, Server Info: {ServerInfo}", session.EndPoint, session.ConnectionInfo);
                    }
                    else
                    {
                        _logger.LogInformation("New TLS connection from {EndPoint}", session.EndPoint);
                    }

                    var connectionInfo = new CoapDtlsConnectionInformation
                    {
                        LocalEndpoint = _endPoint,
                        RemoteEndpoint = session,
                        TlsServer = server
                    };

                    while (!session.IsClosed && !_cts.IsCancellationRequested)
                    {
                        var packet = await session.ReceiveAsync(_cts.Token);
                        _logger.LogDebug("Handling CoAP Packet from {EndPoint}", session.EndPoint);
                        await _coapHandler.ProcessRequestAsync(connectionInfo, packet.Payload);
                        _logger.LogDebug("CoAP request from {EndPoint} handled!", session.EndPoint);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (DtlsConnectionClosedException)
                {
                }
                catch (TlsFatalAlert tlsAlert)
                {
                    if (!(tlsAlert.InnerException is DtlsConnectionClosedException) && tlsAlert.AlertDescription != AlertDescription.user_canceled)
                    {
                        _logger.LogWarning(tlsAlert, "TLS Error");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while handling session");
                }
                finally
                {
                    _logger.LogInformation("Connection from {EndPoint} closed", session.EndPoint);
                    _sessions.TryRemove(session.EndPoint, out _);
                }
            }
        }

        private async Task HandleCleanup()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    int cleaned = 0;
                    foreach (var session in _sessions.Values)
                    {
                        if (session.LastReceivedTime < DateTime.UtcNow - _sessionTimeout)
                        {
                            session.Dispose();
                            cleaned++;
                        }
                    }

                    if (cleaned > 0)
                    {
                        _logger.LogInformation("Closed {CleanedSessions} stale sessions", cleaned);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while cleaning up sessions");
                }
                await Task.Delay(10000);
            }
        }

        public Task UnbindAsync()
        {
            _socket?.Close();
            _socket?.Dispose();
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            foreach (var session in _sessions.Values)
            {
                try
                {
                    session.Dispose();
                }
                catch
                {
                    // Ignore all errors since we can not do anything anyways.
                }
            }

            _cts.Cancel();
            return Task.CompletedTask;
        }
    }
}
