using System;
using System.Collections.Generic;
using System.Text;

using CoAP;

namespace CoAP.Net.Options
{
    public class UriHost : CoapOption
    {
        public UriHost() : base(optionNumber: RegisteredOptionNumber.UriHost, minLength: 1, maxLength: 255, type: OptionType.String)
        {
            ValueString = null;

        }

        public UriHost(string value) : this()
        {
            ValueString = value;
        }
    }

    public class UriPort : CoapOption
    {
        public UriPort() : base(optionNumber: RegisteredOptionNumber.UriPort, minLength: 0, maxLength: 2, type: OptionType.UInt)
        {
            ValueUInt = 0u;
        }

        public UriPort(ushort value) : this()
        {
            ValueUInt = value;
        }
    }

    public class UriPath : CoapOption
    {
        public UriPath() : base(optionNumber: RegisteredOptionNumber.UriPath, minLength: 0, maxLength: 255, isRepeatable: true, type: OptionType.String)
        {
            ValueString = null;
        }

        public UriPath(string value) : this()
        {
            ValueString = value;
        }
    }

    public class UriQuery : CoapOption
    {
        public UriQuery() : base(optionNumber: RegisteredOptionNumber.UriQuery, minLength: 0, maxLength: 255, isRepeatable: true, type: OptionType.String)
        {
            ValueString = null;
        }
        public UriQuery(string value) : this()
        {
            ValueString = value;
        }
    }
}
