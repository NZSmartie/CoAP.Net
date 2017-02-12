using System;
using System.Collections.Generic;
using System.Text;

namespace CoAP.Net.Options
{
    /// <summary>
    /// <para>See section 5.10.7 of [RFC7252]</para>
    /// </summary>
    public class LocationPath : Option
    {
        public LocationPath() : base(optionNumber: 8, maxLength: 255, isRepeatable: true, type: OptionType.String) { }

        public override string GetDefaultString()
        {
            return null;
        }
    }

    /// <summary>
    /// <para>See section 5.10.7 of [RFC7252]</para>
    /// </summary>
    public class LocationQuery : Option
    {
        public LocationQuery() : base(optionNumber: 20, maxLength: 255, isRepeatable: true, type: OptionType.String){ }

        public override string GetDefaultString()
        {
            return null;
        }
    }
}
