using System;
using System.Collections.Generic;
using System.Text;

namespace CoAPNet
{
#if !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5
    /// <summary>
    ///  A customizable parser based on the CoAP scheme.
    /// </summary>
    public class CoapStyleUriParser : HttpStyleUriParser
    {
        /// <summary>
        /// Register CoaP and CoAPS scheme with <see cref="UriParser"/> if they are not registered already 
        /// </summary>
        /// <remarks>The <see cref="CoapStyleUriParser"/> must be registered with <see cref="UriParser"/> before <see cref="Uri"/> can be used with CoAP URIs.</remarks>
        public static void Register()
        {
            if (!IsKnownScheme("coap"))
                Register(new CoapStyleUriParser(), "coap", Coap.Port);
            if (!IsKnownScheme("coaps"))
                Register(new CoapStyleUriParser(), "coaps", Coap.PortDTLS);
        }
    }
#endif
}
