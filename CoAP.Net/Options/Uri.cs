using System;
using System.Collections.Generic;
using System.Text;

using CoAP;

namespace CoAP.Net.Options
{
    public class UriHost : Option
    {
        public UriHost() : base(optionNumber: 3, minLength: 1, maxLength: 255, type: OptionType.String) { }

        public override string GetDefaultString()
        {
            return null;
        }
    }

    public class UriPort : Option
    {
        public UriPort() : base(optionNumber: 7, minLength: 0, maxLength: 2, type: OptionType.UInt) { }

        public override uint GetDefaultUInt()
        {
            return 0;
        }
    }

    public class UriPath : Option
    {
        public UriPath() : base(optionNumber: 11, minLength: 0, maxLength: 255, isRepeatable: true, type: OptionType.String) { }

        public override string GetDefaultString()
        {
            return null;
        }
    }

    public class UriQuery : Option
    {
        public UriQuery() : base(optionNumber: 15, minLength: 0, maxLength: 255, isRepeatable: true, type: OptionType.String) { }

        public override string GetDefaultString()
        {
            return null;
        }
    }
}
