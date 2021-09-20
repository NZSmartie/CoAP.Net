using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Tls;

namespace CoAPNet.Dtls.Server
{
    internal class CoapDtlsServerClientEndPoint : ICoapEndpoint
    {
        private readonly QueueDatagramTransport _udpTransport;

        private DtlsTransport _dtlsTransport;

        public CoapDtlsServerClientEndPoint(IPEndPoint endPoint, QueueDatagramTransport udpTransport, DateTime sessionStartTime)
        {
            BaseUri = new UriBuilder()
            {
                Scheme = "coaps://",
                Host = endPoint.Address.ToString(),
                Port = endPoint.Port
            }.Uri;

            EndPoint = endPoint;
            _udpTransport = udpTransport;
            SessionStartTime = sessionStartTime;
        }

        public IPEndPoint EndPoint { get; }
        public Uri BaseUri { get; }
        public string ConnectionInfo { get; private set; }

        public bool IsSecure => true;

        public bool IsMulticast => false;

        public DateTime SessionStartTime { get; }
        public DateTime LastReceivedTime { get; private set; }
        public bool IsClosed { get; private set; }

        public void Dispose()
        {
            _dtlsTransport?.Close();
            IsClosed = true;
        }

        public async Task<CoapPacket> ReceiveAsync(CancellationToken token)
        {
            var bufLen = _dtlsTransport.GetReceiveLimit();
            var buffer = new byte[bufLen];
            while (!token.IsCancellationRequested)
            {
                if (_udpTransport.IsClosed || _dtlsTransport == null)
                    throw new DtlsConnectionClosedException();

                // we can't cancel waiting for a packet (BouncyCastle doesn't support this), so there will be a bit of delay between cancelling and actually stopping trying to receive.
                // there is a wait timeout of 5000ms to close the CoapEndPoint, this has to be less than that.
                // also, we use a long running task here so we don't block the calling thread till we're done waiting, but start a new one and yield instead
                int received = await Task.Factory.StartNew(() => _dtlsTransport.Receive(buffer, 0, bufLen, 4000), TaskCreationOptions.LongRunning);

                if (received > 0)
                {
                    return await Task.FromResult(new CoapPacket
                    {
                        Payload = new ArraySegment<byte>(buffer, 0, received).ToArray(),
                        Endpoint = this
                    });
                }
            }

            throw new OperationCanceledException();
        }

        public Task SendAsync(CoapPacket packet, CancellationToken token)
        {
            if (!_udpTransport.IsClosed && _dtlsTransport != null)
                _dtlsTransport.Send(packet.Payload, 0, packet.Payload.Length);
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            return ToString(CoapEndpointStringFormat.Simple);
        }

        public string ToString(CoapEndpointStringFormat format)
        {
            return EndPoint.ToString();
        }

        public void Accept(DtlsServerProtocol serverProtocol, TlsServer server)
        {
            _dtlsTransport = serverProtocol.Accept(server, _udpTransport);

            if (server is IDtlsServerWithConnectionInfo serverWithInfo)
            {
                var serverInfo = serverWithInfo.GetConnectionInfo();
                ConnectionInfo = serverInfo;
            }
        }

        public void EnqueueDatagram(byte[] datagram)
        {
            if (!IsClosed)
                _udpTransport.ReceiveQueue.Add(datagram);
            LastReceivedTime = DateTime.UtcNow;
        }
    }
}
