using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CoAPNet
{
    /// <summary>
    /// Represents CoAP Block-Wise Transfer specific errors that occur during application execution.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CoapBlockException : CoapException
    {
        /// <inheritdoc/>
        public CoapBlockException()
        { }

        /// <inheritdoc/>
        public CoapBlockException(string message) 
            : base(message)
        { }

        /// <inheritdoc/>
        public CoapBlockException(string message, CoapMessageCode responseCode) 
            : base(message, responseCode)
        { }

        /// <inheritdoc/>
        public CoapBlockException(string message, Exception innerException) 
            : base(message, innerException)
        { }

        /// <inheritdoc/>
        public CoapBlockException(string message, Exception innerException, CoapMessageCode responseCode) 
            : base(message, innerException, responseCode)
        { }
    }
}
