using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CoAPNet.Udp
{
    public class CoapConnectionInformation : ICoapConnectionInformation
    {
        public ICoapEndpoint LocalEndpoint { get; set; }
        public ICoapEndpoint RemoteEndpoint { get; set; }
    }

    public class CoapUdpEndPoint : ICoapEndpoint
    {
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

        public CoapUdpEndPoint(UdpClient udpClient)
        {
            Client = udpClient ?? throw new ArgumentNullException(nameof(udpClient));
            _endpoint = (IPEndPoint)Client.Client.LocalEndPoint;

            BaseUri = new UriBuilder()
            {
                Scheme = "coap://",
                Host = _endpoint.Address.ToString(),
                Port = _endpoint.Port != Coap.Port ? _endpoint.Port : -1
            }.Uri;
        }

        public CoapUdpEndPoint(IPEndPoint endpoint)
        {
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
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Client?.Dispose();
        }

        public async Task<CoapPacket> ReceiveAsync(CancellationToken token = default (CancellationToken))
        {
            if (Client == null)
                throw new InvalidOperationException();

            var result = await Client.ReceiveAsync();
            return new CoapPacket
            {
                Payload = result.Buffer,
                Endpoint = new CoapUdpEndPoint(result.RemoteEndPoint) {Bindable = false},
            };
        }

        public async Task SendAsync(CoapPacket packet, CancellationToken token = default (CancellationToken))
        {
            if (Client == null)
                throw new InvalidOperationException();

            var udpDestEndpoint = packet.Endpoint as CoapUdpEndPoint;
            if (udpDestEndpoint == null)
                throw new ArgumentException();

            try
            {
                await Client.SendAsync(packet.Payload, packet.Payload.Length, udpDestEndpoint.Endpoint);
            }
            catch (SocketException se)
            {
                Debug.WriteLine($"Failed to send data. {se.GetType().FullName} (0x{se.HResult:x}): {se.Message}");
            }
        }

        public override string ToString()
        {
            return _endpoint.ToString();
        }
    }
}
