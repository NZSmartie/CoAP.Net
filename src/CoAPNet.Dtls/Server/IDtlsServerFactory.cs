using Org.BouncyCastle.Crypto.Tls;

namespace CoAPNet.Dtls.Server
{
    public interface IDtlsServerFactory
    {
        TlsServer Create();
    }
}
