using System;
using System.Collections.Generic;
using System.Text;

namespace CoAP.Net.Options
{
    public class ProxyUri : Option
    {
        public ProxyUri() : base(optionNumber: 35, minLength: 1, maxLength: 1034, type: OptionType.String) { }

        public override string GetDefaultString()
        {
            return null;
        }
    }

    public class ProxyScheme : Option
    {
        public ProxyScheme() : base(optionNumber: 39, minLength: 1, maxLength: 255, type: OptionType.String) { }

        public override string GetDefaultString()
        {
            return null;
        }
    }
}
