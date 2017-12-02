using System;
using System.Collections.Generic;
using System.Text;

namespace CoAPNet
{
    public class CoapBlockException : CoapException
    {
        public CoapBlockException()
        { }

        public CoapBlockException(string message) 
            : base(message)
        { }

        public CoapBlockException(string message, CoapMessageCode responseCode) 
            : base(message, responseCode)
        { }

        public CoapBlockException(string message, Exception innerException) 
            : base(message, innerException)
        { }

        public CoapBlockException(string message, Exception innerException, CoapMessageCode responseCode) 
            : base(message, innerException, responseCode)
        { }
    }
}
