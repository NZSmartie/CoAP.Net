using System;
using System.Collections.Generic;
using System.Text;

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

    class Message
    {
        private byte[] _header;

        public int Version { get; set;  }
        public MessageType Type { get; set; }
        public byte[] Token { get; set; }
        public int Id { get; set; }

        public List<Option> Options { get; set; }

        public Message()
        {
            
        }

    }
}
