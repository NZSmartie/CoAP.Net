using System;

namespace CoAPNet.Dtls.Server
{
    internal class DtlsConnectionClosedException : Exception
    {
        public DtlsConnectionClosedException()
        {
        }

        public DtlsConnectionClosedException(string message)
            : base(message)
        {
        }

        public DtlsConnectionClosedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
