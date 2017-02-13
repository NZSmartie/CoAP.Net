using System;
using System.Collections.Generic;
using System.Linq;

namespace CoAP.Net
{
    public enum MessageType
    {
        Confirmable = 0,
        Nonnonfirmable = 1,
        Acknowlledgemetn = 2,
        Reset = 3
    }

    public enum Method
    {
        Get,
        Post,
        Put,
        Delete
    }

    public enum ResponseCodeClass
    {
        None = 0,
        Success = 200,
        ClientError = 400,
        ServerError = 500
    }

    /// <summary>
    /// Response Codes
    /// <para>See section 5.9 of [RFC7252]</para>
    /// </summary>
    public enum ResponseCode
    {
        None = 0,
        Created = 201,
        Deleted = 202,
        Valid = 203,
        Changed = 204,
        Content = 205,
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
        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503,
        GatewayTimeout = 504,
        ProxyingNotSupported = 505
    }

    public class Message
    {
        private byte[] _header;

        public int Version { get; set; }
        public MessageType Type { get; set; }
        public byte[] Token { get; set; }
        public int Id { get; set; }

        private List<Option> _options = new List<Option>();

        public List<Option> Options
        {
            get { return _options; }
            set { _options = value; }
        }

        public Message() { }

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
                _options.Add(new Options.UriPort { ValueUInt = (ushort)uri.Port });

            _options.AddRange(uri.AbsolutePath.Substring(1).Split(new[] { '/' }).Select(p => new Options.UriPath { ValueString = Uri.UnescapeDataString(p) }));

            if (uri.Query.Length > 0)
                _options.AddRange(uri.Query.Substring(1).Split(new[] { '&' }).Select(p => new Options.UriQuery { ValueString = Uri.UnescapeDataString(p) }));
        }
    }
}
