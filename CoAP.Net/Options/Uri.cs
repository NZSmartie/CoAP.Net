﻿using System;
using System.Collections.Generic;
using System.Text;

using CoAP;

namespace CoAP.Net.Options
{
    public class UriHost : Option
    {
        public UriHost(string value = null) : base(optionNumber: RegisteredOptionNumber.UriHost, minLength: 1, maxLength: 255, type: OptionType.String) {
            ValueString = value;
        }
    }

    public class UriPort : Option
    {
        public UriPort(ushort value = 0) : base(optionNumber: RegisteredOptionNumber.UriPort, minLength: 0, maxLength: 2, type: OptionType.UInt) {
            ValueUInt = value;
        }
    }

    public class UriPath : Option
    {
        public UriPath(string value = null) : base(optionNumber: RegisteredOptionNumber.UriPath, minLength: 0, maxLength: 255, isRepeatable: true, type: OptionType.String) {
            ValueString = value;
        }
    }

    public class UriQuery : Option
    {
        public UriQuery(string value = null) : base(optionNumber: RegisteredOptionNumber.UriQuery, minLength: 0, maxLength: 255, isRepeatable: true, type: OptionType.String) {
            ValueString = value;
        }
    }
}