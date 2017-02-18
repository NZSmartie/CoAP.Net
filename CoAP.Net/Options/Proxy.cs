﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CoAP.Net.Options
{
    public class ProxyUri : Option
    {
        public ProxyUri() : base(optionNumber: RegisteredOptionNumber.ProxyUri, minLength: 1, maxLength: 1034, type: OptionType.String) { }
    }

    public class ProxyScheme : Option
    {
        public ProxyScheme() : base(optionNumber: RegisteredOptionNumber.ProxyScheme, minLength: 1, maxLength: 255, type: OptionType.String) { }
    }
}