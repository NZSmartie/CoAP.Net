using System;
using System.Collections.Generic;

using CoAPNet;

namespace CoAPNet.Options
{
    /// <summary>
    /// Internet media types are identified by a string, such as
    /// "application/xml" [RFC2046].  In order to minimize the overhead of using these media types to 
    /// indicate the format of payloads, a sub-registry for a subset of Internet media types
    /// are used in CoAP and are assigned a numeric identifier. The name of the sub-registry is "CoAP
    /// Content-Formats", within the "CoRE Parameters" registry.
    /// <para>See section 12.3 of [RFC7252]</para>
    /// </summary>
    public sealed class ContentFormatType
    {
        private static Dictionary<string, ContentFormatType> nameLookup = new Dictionary<string, ContentFormatType>();
        private static Dictionary<int, ContentFormatType> valueLookup = new Dictionary<int, ContentFormatType>();

        /// <summary>
        /// text/plain; charset=utf-8
        /// <para>[RFC2046] [RFC3676] [RFC5147]</para>
        /// </summary>
        public static readonly ContentFormatType TextPlain = new ContentFormatType(0, "text/plain");
        /// <summary>
        /// application/link-format
        /// <para>See [rfc6690]</para>
        /// </summary>
        public static readonly ContentFormatType ApplicationLinkFormat = new ContentFormatType(40, "application/link-format");
        /// <summary>
        /// application/xml
        /// <para>[RFC3023]</para>
        /// </summary>
        public static readonly ContentFormatType ApplicationXml = new ContentFormatType(41, "application/xml");
        /// <summary>
        /// application/octet-stream
        /// <para>See [RFC2045] and [RFC2046]</para>
        /// </summary>
        public static readonly ContentFormatType ApplicationOctetStream = new ContentFormatType(42, "application/octet-stream");
        /// <summary>
        /// application/exi
        /// <para>See [REC-exi-20140211]</para>
        /// </summary>
        public static readonly ContentFormatType ApplicationExi = new ContentFormatType(47, "application/exi");
        /// <summary>
        /// application/json
        /// <para>See [RFC7159]</para>
        /// </summary>
        public static readonly ContentFormatType ApplicationJson = new ContentFormatType(50, "application/json");
        /// <summary>
        /// applicaiton/cbor
        /// <para>See [RFC7049]</para>
        /// </summary>
        public static readonly ContentFormatType ApplicationCbor = new ContentFormatType(60, "applicaiton/cbor");

        private readonly int _value;
        private readonly string _name;

        /// <summary>
        /// Gets the numeric value that represents the CoAP content format option.
        /// </summary>
        public int Value => _value;

        /// <summary>
        /// Gets the human-readable name of the CoAP content format option.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Creates a new <see cref="ContentFormatType"/> to be used when setting the <see cref="ContentFormat"/> Option when used with <see cref="CoapMessage.Options"/>
        /// </summary>
        /// <param name="value">The numeric value that represents the CoAP content format option.</param>
        /// <param name="name">The human-readable name of the CoAP content format option.</param>
        public ContentFormatType(int value, string name)
        {
            _value = value;
            _name = name;

            nameLookup[_name] = this;
            valueLookup[_value] = this;
        }

        #region implicit operators (string, int, uint)

        public static implicit operator ContentFormatType(string name)
        {
            if (nameLookup.TryGetValue(name, out var result))
                return result;
            throw new CoapOptionException($"Unsupported content format \"{name}\"");
        }

        public static implicit operator ContentFormatType(int value)
        {
            if (valueLookup.TryGetValue(value, out var result))
                return result;
            throw new CoapOptionException($"Unsupported content format ({value})");
        }

        public static implicit operator ContentFormatType(uint value)
        {
            return ((int)value);
        }

        public static implicit operator int(ContentFormatType type)
        {
            return type._value;
        }

        public static implicit operator uint(ContentFormatType type)
        {
            return (uint)type._value;
        }

        #endregion

        /// <inheritdoc />
        public override string ToString()
        {
            return _name;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            var type = obj as ContentFormatType;
            return type != null &&
                   _value == type._value;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return -1939223833 + _value.GetHashCode();
        }
    }

    /// <summary>
    /// The Content-Format Option indicates the representation format of the message payload. The representation format is given as a numeric
    /// Content-Format identifier that is defined in the "CoAP Content-Formats" registry (Section 12.3 of [RFC7252]). 
    /// <para>See section 5.10.3 of [RFC7252]</para>
    /// </summary>
    public class ContentFormat : CoapUintOption
    {
        public ContentFormatType MediaType { get => (ContentFormatType)ValueUInt; set => ValueUInt = (uint)value; }

        public ContentFormat() : base(optionNumber: CoapRegisteredOptionNumber.ContentFormat, maxLength: 2)
        {
            MediaType = ContentFormatType.TextPlain;
        }

        public ContentFormat(ContentFormatType type) : this()
        {
            MediaType = type;
        }
    }

    /// <summary>
    /// The CoAP Accept option can be used to indicate which Content-Format is acceptable to the client.
    /// The representation format is given as a numeric Content-Format identifier that is defined in the 
    /// "CoAP Content-Formats" registry (Section 12.3 of [RFC7252]).
    /// <para>See section 5.10.4 of [RFC7252]</para>
    /// </summary>
    public class Accept : CoapUintOption
    {
        public ContentFormatType MediaType { get => (ContentFormatType)ValueUInt; set => ValueUInt = (uint)value; }

        public Accept() : base(optionNumber: CoapRegisteredOptionNumber.Accept, maxLength: 2)
        {
            MediaType = ContentFormatType.TextPlain;
        }

        public Accept(ContentFormatType type) : this()
        {
            MediaType = type;
        }
    }

    /// <summary>
    /// The Max-Age Option indicates the maximum time a response may be
    /// cached before it is considered not fresh(see Section 5.6.1 of [RFC7252]).
    /// <para>The option value is an integer number of seconds between 0 and
    /// 2**32-1 inclusive(about 136.1 years). A default value of 60 seconds
    /// is assumed in the absence of the option in a response.</para>
    /// <para>The value is intended to be current at the time of transmission.
    /// Servers that provide resources with strict tolerances on the value of
    /// Max-Age SHOULD update the value before each retransmission.  (See also Section 5.7.1. of [RFC7252])</para>
    /// <para>See section 5.10.5 of [RFC7252]</para>
    /// </summary>
    public class MaxAge : CoapUintOption
    {
        public MaxAge() : base(optionNumber: CoapRegisteredOptionNumber.MaxAge, maxLength: 4, defaultValue: 60u)
        {
            ValueUInt = 0u;
        }

        public MaxAge(uint value) : this()
        {
            ValueUInt = value;
        }
    }

    /// <summary>
    /// An entity-tag is intended for use as a resource-local identifier for
    /// differentiating between representations of the same resource that
    /// vary over time.It is generated by the server providing the
    /// resource, which may generate it in any number of ways including a
    /// version, checksum, hash, or time.An endpoint receiving an entity-
    /// tag MUST treat it as opaque and make no assumptions about its content
    /// or structure.  (Endpoints that generate an entity-tag are encouraged
    /// to use the most compact representation possible, in particular in
    /// regards to clients and intermediaries that may want to store multiple
    /// ETag values.)
    /// <para>See section 5.10.6 of [RFC7252]</para>
    /// </summary>
    /// TODO: Implement ETag request/response semantics as descripbed in section 5.10.6.1 and 5.10.6.2 of [RFC7252]
    public class ETag : CoapOpaqueOption
    {
        public ETag() : base(optionNumber: CoapRegisteredOptionNumber.ETag, minLength: 1, maxLength: 8, isRepeatable: true)
        { 
            ValueOpaque = null;
        }

        public ETag(byte[] value) : this()
        {
            ValueOpaque = value;
        }
    }

    /// <summary>
    /// The Size1 option provides size information about the resource
    /// representation in a request.The option value is an integer number
    /// of bytes.Its main use is with block-wise transfers [BLOCK].  In the
    /// present specification, it is used in 4.13 responses (Section 5.9.2.9)
    /// to indicate the maximum size of request entity that the server is
    /// able and willing to handle.
    /// <para>See section 5.10.9 of [RFC7252]</para>
    /// </summary>
    public class Size1 : CoapUintOption
    {
        public Size1() : base(optionNumber: CoapRegisteredOptionNumber.Size1, maxLength: 4, defaultValue: 0u)
        { }

        public Size1(uint value) : this()
        {
            ValueUInt = value;
        }
    }
}
