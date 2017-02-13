using System;
using System.Collections.Generic;
using System.Text;

using CoAP;

namespace CoAP.Net.Options
{
    public class UriHost : Option
    {
        public UriHost() : base(optionNumber: RegisteredOptionNumber.UriHost, minLength: 1, maxLength: 255, type: OptionType.String) { }
    }

    public class UriPort : Option
    {
        public UriPort() : base(optionNumber: RegisteredOptionNumber.UriPort, minLength: 0, maxLength: 2, type: OptionType.UInt) { }
    }

    public class UriPath : Option
    {
        public UriPath() : base(optionNumber: RegisteredOptionNumber.UriPath, minLength: 0, maxLength: 255, isRepeatable: true, type: OptionType.String) { }
    }

    public class UriQuery : Option
    {
        public UriQuery() : base(optionNumber: RegisteredOptionNumber.UriQuery, minLength: 0, maxLength: 255, isRepeatable: true, type: OptionType.String) { }
    }
}
