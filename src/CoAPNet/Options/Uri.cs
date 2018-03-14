using System;
using System.Collections.Generic;
using System.Text;

using CoAPNet;

namespace CoAPNet.Options
{
    public class UriHost : CoapStringOption
    {
        public UriHost() : base(optionNumber: CoapRegisteredOptionNumber.UriHost, minLength: 1, maxLength: 255)
        {
            ValueString = null;

        }

        public UriHost(string value) : this()
        {
            ValueString = value;
        }
    }

    public class UriPort : CoapUintOption
    {
        public UriPort() : base(optionNumber: CoapRegisteredOptionNumber.UriPort, minLength: 0, maxLength: 2)
        {
            ValueUInt = 0u;
        }

        public UriPort(ushort value) : this()
        {
            ValueUInt = value;
        }
    }

    public class UriPath : CoapStringOption
    {
        public UriPath() : base(optionNumber: CoapRegisteredOptionNumber.UriPath, minLength: 0, maxLength: 255, isRepeatable: true)
        {
            ValueString = null;
        }

        public UriPath(string value) : this()
        {
            ValueString = value;
        }
    }

    public class UriQuery : CoapStringOption
    {
        public UriQuery() : base(optionNumber: CoapRegisteredOptionNumber.UriQuery, minLength: 0, maxLength: 255, isRepeatable: true)
        {
            ValueString = null;
        }
        public UriQuery(string value) : this()
        {
            ValueString = value;
        }
    }
}
