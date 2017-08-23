using System;
using System.Collections.Generic;
using System.Text;

namespace CoAPNet.Options
{
    public class ProxyUri : CoapOption
    {
        public ProxyUri() : base(optionNumber: CoapRegisteredOptionNumber.ProxyUri, minLength: 1, maxLength: 1034, type: OptionType.String) { }
    }

    public class ProxyScheme : CoapOption
    {
        public ProxyScheme() : base(optionNumber: CoapRegisteredOptionNumber.ProxyScheme, minLength: 1, maxLength: 255, type: OptionType.String) { }
    }
}
