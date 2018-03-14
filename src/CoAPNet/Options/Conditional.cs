using System;
using System.Collections.Generic;
using System.Text;

namespace CoAPNet.Options
{
    /// <summary>
    /// The If-Match Option MAY be used to make a request conditional on the
    /// current existence or value of an ETag for one or more representations
    /// of the target resource.If-Match is generally useful for resource
    /// update requests, such as PUT requests, as a means for protecting
    /// against accidental overwrites when multiple clients are acting in
    /// parallel on the same resource (i.e., the "lost update" problem).
    /// <para>See section 5.10.8.1 of [RFC7252]</para>
    /// </summary>
    public class IfMatch : CoapOpaqueOption
    {
        public IfMatch() : base(optionNumber: CoapRegisteredOptionNumber.IfMatch, maxLength: 8, isRepeatable: true) { }
    }

    /// <summary>
    /// The If-None-Match Option MAY be used to make a request conditional on
    /// the nonexistence of the target resource.If-None-Match is useful for
    /// resource creation requests, such as PUT requests, as a means for
    /// protecting against accidental overwrites when multiple clients are
    /// acting in parallel on the same resource.The If-None-Match Option
    /// carries no value.
    /// <para>See section 5.10.8.2 of [RFC7252]</para>
    /// </summary>
    public class IfNoneMatch : CoapEmptyOption
    {
        public IfNoneMatch() : base(optionNumber: CoapRegisteredOptionNumber.IfNoneMatch) { }
    }
}
