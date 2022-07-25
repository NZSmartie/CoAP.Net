using Org.BouncyCastle.Tls;

namespace CoAPNet.Dtls.Server
{
    public interface IDtlsServerFactory
    {
        TlsServer Create();
    }
}
