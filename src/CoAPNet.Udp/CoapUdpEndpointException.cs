using System;
using System.Collections.Generic;
using System.Text;

namespace CoAPNet.Udp
{
    public class CoapUdpEndpointException : CoapEndpointException
    {
        public CoapUdpEndpointException()
            : base()
        { }

        public CoapUdpEndpointException(string message) 
            : base(message)
        { }

        public CoapUdpEndpointException(string message, Exception innerException) 
            : base(message, innerException)
        { }
    }
}
