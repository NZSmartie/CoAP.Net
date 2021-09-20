using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CoAPNet.Dtls.Server
{
    public class CoapDtlsServerEndPoint : ICoapEndpoint
    {
        public bool IsSecure => true;

        public bool IsMulticast => false;

        public Uri BaseUri { get; }
        public IPEndPoint IPEndPoint { get; }

        public CoapDtlsServerEndPoint(IPAddress address = null, int port = Coap.PortDTLS)
        {
            address = address ?? IPAddress.IPv6Any;

            BaseUri = new UriBuilder()
            {
                Scheme = "coaps://",
                Host = address.ToString(),
                Port = port
            }.Uri;

            IPEndPoint = new IPEndPoint(address, port);
        }

        public void Dispose()
        {
        }

        public Task<CoapPacket> ReceiveAsync(CancellationToken tokens)
        {
            throw new InvalidOperationException("Receiving can only be done via a DTLS session");
        }

        public async Task SendAsync(CoapPacket packet, CancellationToken token)
        {
            //packet has the CoapDtlsServerClientEndPoint which we have to respond to.
            await packet.Endpoint.SendAsync(packet, token);
        }

        public string ToString(CoapEndpointStringFormat format)
        {
            return BaseUri.ToString();
        }
    }
}
