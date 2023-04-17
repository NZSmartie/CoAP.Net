using Org.BouncyCastle.Tls;

namespace CoAPNet.Dtls.Server
{
    public interface IDtlsServerWithConnectionId : TlsServer
    {
        byte[] GetConnectionId();
    }
}
