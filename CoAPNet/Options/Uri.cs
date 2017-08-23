using System;
using System.Collections.Generic;
using System.Text;

using CoAPNet;

namespace CoAPNet.Options
{
    public class UriHost : CoapOption
    {
        public UriHost() : base(optionNumber: CoapRegisteredOptionNumber.UriHost, minLength: 1, maxLength: 255, type: OptionType.String)
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
        public UriPort() : base(optionNumber: CoapRegisteredOptionNumber.UriPort, minLength: 0, maxLength: 2, type: OptionType.UInt)
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
        public UriPath() : base(optionNumber: CoapRegisteredOptionNumber.UriPath, minLength: 0, maxLength: 255, isRepeatable: true, type: OptionType.String)
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
        public UriQuery() : base(optionNumber: CoapRegisteredOptionNumber.UriQuery, minLength: 0, maxLength: 255, isRepeatable: true, type: OptionType.String)
        {
            ValueString = null;
        }
        public UriQuery(string value) : this()
        {
            ValueString = value;
        }
    }
}
