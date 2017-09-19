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
            : this(new IPEndPoint(IPAddress.Any, port), logger)
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


            Client = new UdpClient(_endpoint) { EnableBroadcast = true };

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

        public async Task<CoapPacket> ReceiveAsync()
        {
            if (Client == null)
                await BindAsync();

            var result = await Client.ReceiveAsync();
            return new CoapPacket
            {
                Payload = result.Buffer,
                Endpoint = new CoapUdpEndPoint(result.RemoteEndPoint) {Bindable = false},
            };
        }

        public async Task SendAsync(CoapPacket packet)
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

                    udpDestEndpoint = new CoapUdpEndPoint(coapEndpoint.BaseUri.Host, port);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported {nameof(CoapPacket)}.{nameof(CoapPacket.Endpoint)} type ({packet.Endpoint.GetType().FullName})");
            }

            try
            {
                await Client.SendAsync(packet.Payload, packet.Payload.Length, udpDestEndpoint.Endpoint);
            }
            catch (SocketException se)
            {
                _logger?.LogInformation($"Failed to send data. {se.GetType().FullName} (0x{se.HResult:x}): {se.Message}", se);
            }
        }

        public override string ToString()
        {
            return _endpoint.ToString();
        }
    }
}
