using System;
using System.Collections.Generic;
using System.Linq;

namespace CoAP.Net
{
    public enum MessageType
    {
        Confirmable = 0,
        Nonnonfirmable = 1,
        Acknowlledgement = 2,
        Reset = 3,
    }

    public enum ResponseCodeClass
    {
        Request = 0,
        Success = 200,
        ClientError = 400,
        ServerError = 500
    }

    /// <summary>
    /// Response Codes
    /// <para>See section 5.9 of [RFC7252] and section 12.1 of [RFC7252]</para>
    /// </summary>
    public enum MessageCode
    {
        None = 0,
        // 0.xx Request
        Get = 1,
        Post = 2,
        Put = 3,
        Delete = 4,
        // 2.xx Success
        Created = 201,
        Deleted = 202,
        Valid = 203,
        Changed = 204,
        Content = 205,
        // 4.xx Client Error
        BadRequest = 400,
        Unauthorized = 401,
        BadOption = 402,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        NotAcceptable = 406,
        PreconditionFailed = 412,
        RequestEntityTooLarge = 413,
        UnsupportedContentFormat = 415,
        // 5.xx Server Error
        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503,
        GatewayTimeout = 504,
        ProxyingNotSupported = 505
    }

    public class Message
    {

        private int _version = 1;
        public int Version
        {
            get { return _version; }
            set
            {
                if (value != 1)
                    throw new ArgumentException("Only version 1 is supported");
                _version = value;
            }
        }

        public MessageType Type { get; set; }
        public MessageCode Code { get; set; }

        private byte[] _token = new byte[0];
        public byte[] Token
        {
            get { return _token; }
            set
            {
                if (value.Length > 8)
                    throw new ArgumentException("Token length is too long");
                _token = value;
            }
        }

        public ushort Id { get; set; }

        private List<Option> _options = new List<Option>();

        public List<Option> Options
        {
            get { return _options; }
            set { _options = value; }
        }

        public byte[] Payload { get; set; }

        public Message() { }

        public byte[] Serialise()
        {
            var result = new List<byte>();
            byte optCode = 0;
            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            // |Ver| T |  TKL  |      Code     |           Message ID          |
            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

            var type = (byte)Type;
            result.Add((byte)(0x40 | ((type << 4) & 0x30) | _token.Length)); // Ver | T | TKL

            optCode = (byte)(((int)Code / 100) << 5); // Series
            optCode |= (byte)((int)Code % 100); // Series Code
            result.Add(optCode); // Code

            result.Add((byte)((Id >> 8) & 0xFF)); // Message ID (upper byte)
            result.Add((byte)(Id & 0xFF));        // Message ID (lower byte)

            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            // | Token (if any, TKL bytes) ...
            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            foreach (var tb in _token)
                result.Add(tb);

            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            // | Options (if any) ...
            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            // Todo: encode Options in *ORDER* 
            var currentOptionDelta = 0;
            foreach (var option in _options)
            {
                var optionHeader = new List<byte>();
                int optionDelta = option.OptionNumber - currentOptionDelta;
                currentOptionDelta += optionDelta;

                if (optionDelta >= 269)
                {
                    optCode = 0xE0;
                    optionDelta -= 269;
                    optionHeader.Add((byte)((optionDelta & 0xFF00u) >> 8));
                    optionHeader.Add((byte)(optionDelta & 0xFFu));
                }
                else if (optionDelta >= 13)
                {
                    optCode = 0xD0;
                    optionDelta -= 13;
                    optionHeader.Add((byte)(optionDelta & 0xFFu));
                }
                else
                {
                    optCode = (byte)(optionDelta << 4);
                }

                optionDelta = option.Length;
                if (optionDelta >= 269)
                {
                    result.Add((byte)(optCode | 0x0E));
                    optionDelta -= 269;

                    result.AddRange(optionHeader);
                    result.Add((byte)((optionDelta & 0xFF00u) >> 8));
                    result.Add((byte)(optionDelta & 0xFFu));
                }
                else if (optionDelta >= 13)
                {
                    result.Add((byte)(optCode | 0x0D));
                    optionDelta -= 13;

                    result.AddRange(optionHeader);
                    result.Add((byte)(optionDelta & 0xFFu));
                }
                else
                {
                    result.Add((byte)(optCode | optionDelta));
                    result.AddRange(optionHeader);
                }

                result.AddRange(option.GetBytes());
            }

            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            // |1 1 1 1 1 1 1 1| Payload (if any) ...
            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

            if (Payload != null && Payload.Length > 0)
            {
                result.Add(0xFF);
                result.AddRange(Payload);
            }

            return result.ToArray();

        }

        public void FromUri(string input)
        {
            // Will throw exceptions that the application code can handle
            Uri uri = new Uri(input);

            if (!uri.IsAbsoluteUri)
                throw new UriFormatException("URI is not absolute and unsupported by the CoAP scheme");

            if (uri.Scheme != "coap" && uri.Scheme != "coaps")
                throw new UriFormatException("Input URI scheme is not coap:// or coaps://");

            if (uri.Fragment.Length > 0)
                throw new UriFormatException("Fragments are unsupported in the CoAP scheme");

            // Strip out any existing URI Options 
            var optionsToDiscard = new int[] { RegisteredOptionNumber.UriHost, RegisteredOptionNumber.UriPort, RegisteredOptionNumber.UriPath, RegisteredOptionNumber.UriQuery };
            _options = _options.Where(kv => !optionsToDiscard.Contains(kv.OptionNumber)).ToList();

            switch (uri.HostNameType)
            {
                case UriHostNameType.Dns:
                    _options.Add(new Options.UriHost { ValueString = uri.IdnHost });
                    break;
                case UriHostNameType.IPv4:
                case UriHostNameType.IPv6:
                    //_options.Add(new Options.UriHost { ValueString = uri.Host });
                    break;
                default:
                    throw new UriFormatException("Unknown Hostname");
            }

            if ((uri.Scheme == "coap" && !uri.IsDefaultPort && uri.Port != 5683) ||
                (uri.Scheme == "coaps" && !uri.IsDefaultPort && uri.Port != 5684))
                _options.Add(new Options.UriPort((ushort)uri.Port));

            _options.AddRange(uri.AbsolutePath.Substring(1).Split(new[] { '/' }).Select(p => new Options.UriPath(Uri.UnescapeDataString(p))));

            if (uri.Query.Length > 0)
                _options.AddRange(uri.Query.Substring(1).Split(new[] { '&' }).Select(p => new Options.UriQuery(Uri.UnescapeDataString(p))));
        }
    }
}
