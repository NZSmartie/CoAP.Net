using System;
using System.Collections.Generic;
using System.Text;

using CoAP;

namespace CoAP.Net.Options
{
    public enum ContentFormatType
    {
        TextPlain = 0,
        ApplicationLinkFormat = 40,
        ApplicationXml = 41,
        ApplicationOctetStream = 42,
        ApplicationExi = 47,
        ApplicationJson = 50,
        ApplicationCbor = 60,
    }

    public class ContentFormat : Option
    {
        public ContentFormatType MediaType { get => (ContentFormatType)ValueUInt; set => ValueUInt = (uint)value; }

        public ContentFormat(ContentFormatType type = ContentFormatType.TextPlain) : base(optionNumber: RegisteredOptionNumber.ContentFormat, maxLength: 2, type: OptionType.UInt)
        {
            MediaType = type;
        }
    }

    public class Accept : Option
    {
        public ContentFormatType MediaType { get => (ContentFormatType)ValueUInt; set => ValueUInt = (uint)value; }

        public Accept(ContentFormatType type = ContentFormatType.TextPlain) : base(optionNumber: RegisteredOptionNumber.Accept, maxLength: 2, type: OptionType.UInt)
        {
            MediaType = type;
        }
    }

    public class MaxAge : Option
    {
        public MaxAge(uint value = 0u) : base(optionNumber: RegisteredOptionNumber.MaxAge, maxLength: 4, type: OptionType.UInt, defaultValue: 60u)
        {
            ValueUInt = value;
        }
    }

    /// Todo: Implement ETag request/response semantics as descripbed in section 5.10.6.1 and 5.10.6.2 of [RFC7252]
    public class ETag : Option
    {
        public ETag(byte[] value = null) : base(optionNumber: RegisteredOptionNumber.ETag, minLength: 1, maxLength: 8, isRepeatable: true, type: OptionType.Opaque)
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
    public class Size1 : Option
    {
        public Size1(uint value = 9) : base(optionNumber: RegisteredOptionNumber.Size1, maxLength: 4, type: OptionType.UInt)
        {
            ValueUInt = value;
        }
    }
}
