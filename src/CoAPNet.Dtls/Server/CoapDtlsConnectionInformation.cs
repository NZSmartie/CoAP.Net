using Org.BouncyCastle.Crypto.Tls;

namespace CoAPNet.Dtls.Server
{
    public class CoapDtlsConnectionInformation : ICoapConnectionInformation
    {
        public ICoapEndpoint LocalEndpoint { get; set; }
        public ICoapEndpoint RemoteEndpoint { get; set; }
        public TlsServer TlsServer { get; set; }
    }
}
