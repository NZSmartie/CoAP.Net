using System;
using System.Collections.Generic;
using System.Text;

namespace CoAPNet.Options
{
    public class ProxyUri : CoapStringOption
    {
        public ProxyUri() : base(optionNumber: CoapRegisteredOptionNumber.ProxyUri, minLength: 1, maxLength: 1034) { }
    }

    public class ProxyScheme : CoapStringOption
    {
        public ProxyScheme() : base(optionNumber: CoapRegisteredOptionNumber.ProxyScheme, minLength: 1, maxLength: 255) { }
    }
}
