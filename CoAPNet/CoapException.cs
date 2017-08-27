using System;
using System.Diagnostics.CodeAnalysis;

namespace CoAPNet
{
    [ExcludeFromCodeCoverage]
    public class CoapException : Exception
    {
        public CoapException()
        { }

        public CoapException(string message) : base(message)
        { }

        public CoapException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}