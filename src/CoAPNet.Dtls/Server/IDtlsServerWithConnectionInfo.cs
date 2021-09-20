using Org.BouncyCastle.Crypto.Tls;

namespace CoAPNet.Dtls.Server
{
    /// <summary>
    /// If the DTLS Server implements this interface, the supplied ConnectionInfo is logged once the TLS connection is established.
    /// This can be useful to return the identity coming from the IdentityManager
    /// </summary>
    public interface IDtlsServerWithConnectionInfo : TlsServer
    {
        /// <summary>
        /// Get the connection information for the connection handled by this TLS server.
        /// </summary>
        /// <returns>Information about this connection that should be logged once the connection is established.</returns>
        string GetConnectionInfo();
    }
}
