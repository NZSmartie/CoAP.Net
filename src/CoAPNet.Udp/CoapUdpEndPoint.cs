#region License
// Copyright 2017 Roman Vaughan (NZSmartie)
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CoAPNet.Udp
{
    public class CoapConnectionInformation : ICoapConnectionInformation
    {
        public ICoapEndpoint LocalEndpoint { get; set; }
        public ICoapEndpoint RemoteEndpoint { get; set; }
    }

    public class CoapUdpEndPoint : ICoapEndpoint
    {
        private readonly ILogger<CoapUdpEndPoint> _logger;
        private readonly IPEndPoint _endpoint;
        private readonly IPAddress _multicastAddressIPv4 = IPAddress.Parse(Coap.MulticastIPv4);
        private readonly IPAddress[] _multicastAddressIPv6 = Enumerable.Range(1,13).Select(n => IPAddress.Parse(Coap.GetMulticastIPv6ForScope(n))).ToArray();

        public IPEndPoint Endpoint => (IPEndPoint)Client?.Client.LocalEndPoint ?? _endpoint;

        public UdpClient Client { get; private set; }

        internal bool Bindable { get; set; } = true;

        public Uri BaseUri { get; }

        public bool CanReceive => Client?.Client.LocalEndPoint != null;

        public bool IsMulticast { get; }

        public bool IsSecure => false;

        public bool JoinMulticast { get; set; }

        public CoapUdpEndPoint(UdpClient udpClient, ILogger<CoapUdpEndPoint> logger = null)
            :this((IPEndPoint)udpClient.Client.LocalEndPoint, logger)
        {
            Client = udpClient;
        }

        public CoapUdpEndPoint(int port = 0, ILogger<CoapUdpEndPoint> logger = null)
            : this(new IPEndPoint(IPAddress.IPv6Any, port), logger)
        { }

        public CoapUdpEndPoint(IPAddress address, int port = 0, ILogger<CoapUdpEndPoint> logger = null)
            : this(new IPEndPoint(address, port), logger)
        { }

        public CoapUdpEndPoint(string ipAddress, int port = 0, ILogger<CoapUdpEndPoint> logger = null)
            :this(new IPEndPoint(IPAddress.Parse(ipAddress), port), logger)
        { }

        public CoapUdpEndPoint(IPEndPoint endpoint, ILogger<CoapUdpEndPoint> logger = null)
        {
            _logger = logger;
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            IsMulticast = endpoint.Address.Equals(_multicastAddressIPv4) || _multicastAddressIPv6.Contains(endpoint.Address);

            BaseUri = new UriBuilder()
            {
                Scheme = "coap://",
                Host = _endpoint.Address.ToString(),
                Port = _endpoint.Port != Coap.Port ? _endpoint.Port : -1
            }.Uri;
        }

        public Task BindAsync()
        {
            if (Client != null)
                throw new InvalidOperationException($"Can not bind {nameof(CoapUdpEndPoint)} as it is already bound");
            if(!Bindable)
                throw new InvalidOperationException("Can not bind to remote endpoint");


            Client = new UdpClient(AddressFamily.InterNetworkV6) { EnableBroadcast = true };
            Client.Client.DualMode = true;
            Client.Client.Bind(_endpoint);

            if (JoinMulticast)
            {
                switch (Client.Client.AddressFamily)
                {
                    case AddressFamily.InterNetworkV6:
                        _logger?.LogInformation("TODO: Join multicast group with the correct IPv6 scope.");
                        break;
                    case AddressFamily.InterNetwork:
                        Client.JoinMulticastGroup(_multicastAddressIPv4);
                        break;
                    default:
                        _logger?.LogError($"Can not join multicast group for the address family {Client.Client.AddressFamily:G}.");
                        break;
                }
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Client?.Dispose();
        }

        public async Task<CoapPacket> ReceiveAsync(CancellationToken token)
        {
            if (Client == null)
                await BindAsync();

            try
            {
                var tcs = new TaskCompletionSource<bool>();
                using (token.Register(() => tcs.SetResult(false)))
                {
                    var receiveTask = Client.ReceiveAsync();
                    await Task.WhenAny(receiveTask, tcs.Task);

                    token.ThrowIfCancellationRequested();

                    return new CoapPacket
                    {
                        Payload = receiveTask.Result.Buffer,
                        Endpoint = new CoapUdpEndPoint(receiveTask.Result.RemoteEndPoint) {Bindable = false},
                    };
                }
            }
            catch (OperationCanceledException)
            {
                Client.Dispose(); // Since UdpClient doesn't provide a mechanism for cancelling an async task. the safest way is to dispose the whole object
                throw;
            }
        }

        public async Task SendAsync(CoapPacket packet, CancellationToken token)
        {
            if (Client == null)
                await BindAsync();


            CoapUdpEndPoint udpDestEndpoint;
            switch (packet.Endpoint)
            {
                case CoapUdpEndPoint udpEndPoint:
                    udpDestEndpoint = udpEndPoint;
                    break;
                case CoapEndpoint coapEndpoint:
                    int port = coapEndpoint.BaseUri.Port;
                    if (port == -1)
                        port = coapEndpoint.IsSecure ? Coap.PortDTLS : Coap.Port;

                    IPAddress address = null;
                    if (coapEndpoint.IsMulticast)
                        address = _multicastAddressIPv4;
                    else if (coapEndpoint.BaseUri.HostNameType == UriHostNameType.IPv4 || coapEndpoint.BaseUri.HostNameType == UriHostNameType.IPv6)
                        address = IPAddress.Parse(coapEndpoint.BaseUri.Host);
                    else if (coapEndpoint.BaseUri.HostNameType == UriHostNameType.Dns)
                        // TODO: how do we select the best ip address after looking it up? 
                        address = (await Dns.GetHostAddressesAsync(coapEndpoint.BaseUri.Host)).FirstOrDefault();
                    else
                        throw new CoapUdpEndpointException($"Unsupported Uri HostNameType ({coapEndpoint.BaseUri.HostNameType:G}");

                    // Check is we still don't have an address
                    if (address == null)
                        throw new CoapUdpEndpointException($"Can not resolve host name for {coapEndpoint.BaseUri.Host}");

                    udpDestEndpoint = new CoapUdpEndPoint(address, port); // TODO: Support sending to IPv6 multicast endpoints as well.

                    
                    break;
                default:
                    throw new CoapUdpEndpointException($"Unsupported {nameof(CoapPacket)}.{nameof(CoapPacket.Endpoint)} type ({packet.Endpoint.GetType().FullName})");
            }

            token.ThrowIfCancellationRequested();

            var tcs = new TaskCompletionSource<bool>();
            using (token.Register(() => tcs.SetResult(false)))
            {
                try
                {
                    await Task.WhenAny(Client.SendAsync(packet.Payload, packet.Payload.Length, udpDestEndpoint.Endpoint), tcs.Task);
                    if(token.IsCancellationRequested)
                        Client.Dispose(); // Since UdpClient doesn't provide a mechanism for cancelling an async task. the safest way is to dispose the whole object
                }
                catch (SocketException se)
                {
                    _logger?.LogInformation($"Failed to send data. {se.GetType().FullName} (0x{se.HResult:x}): {se.Message}", se);
                }
            }

            token.ThrowIfCancellationRequested();
        }

        /// <inheritdoc />
        public override string ToString()
         => ToString(CoapEndpointStringFormat.Simple);

        /// <inheritdoc />
        public string ToString(CoapEndpointStringFormat format)
        {
            if (format == CoapEndpointStringFormat.Simple)
                return $"{_endpoint.Address}:{_endpoint.Port}";
            if (format == CoapEndpointStringFormat.Debuggable)
                return $"[ udp://{_endpoint.Address}:{_endpoint.Port} {(IsMulticast ? "(M) " : "")}{(IsSecure ? "(S) " : "")}]";

            throw new ArgumentException(nameof(format));
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if(obj is CoapUdpEndPoint other)
            {
                if (!other._endpoint.Equals(_endpoint))
                    return false;
                if (!other.IsMulticast.Equals(IsMulticast))
                    return false;
                if (!other.IsSecure.Equals(IsSecure))
                    return false;
                return true;
            }
            return base.Equals(obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (_endpoint.GetHashCode() ^ 963144320)
                 ^ (IsMulticast.GetHashCode() ^ 1491585648)
                 ^ (IsSecure.GetHashCode() ^ 1074623538);
        }

    }
}
