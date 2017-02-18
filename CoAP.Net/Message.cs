﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace CoAP.Net
{
    /// <summary>
    /// <see cref="Message.Type"/>
    /// </summary>
    public enum MessageType
    {
        Confirmable = 0,
        Nonnonfirmable = 1,
        Acknowledgement = 2,
        Reset = 3,
    }

    /// <summary>
    /// Class pages used to indicate if a <see cref="MessageCode"/> value is a Request, or a Response or an error.
    /// </summary>
    public enum MessageCodeClass
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
        /// <summary>
        /// Gets or sets the protocol version. 
        /// As of [RFC7252], only version 1 is supported. any other value is reserved.
        /// </summary>
        public int Version
        {
            get => _version;
            set
            {
                if (value != 1)
                    throw new ArgumentException("Only version 1 is supported");
                _version = value;
            }
        }

        /// <summary>
        /// Gets or Sets if the message should be responded to by the server. 
        /// <para>When set to <see cref="MessageType.Reset"/>, this message indicates that a <see cref="MessageType.Confirmable"/> message was rejected by the server endpoint.</para>
        /// <para>When set to <see cref="MessageType.Acknowledgement"/>, this message indicates it was accepted (and responded) by the server enpoint.</para>
        /// </summary>
        public MessageType Type { get; set; }

        /// <summary>
        /// Gets or Sets the Message Code. 
        /// <para>The class indicates if the message is <see cref="MessageCodeClass.Request"/>, <see cref="MessageCodeClass.Success"/>, <see cref="MessageCodeClass.ClientError"/>, or a <see cref="MessageCodeClass.ServerError"/></para>
        /// <para>See section 2.2 of [RFC7252]</para>
        /// </summary>
        public MessageCode Code { get; set; }

        private byte[] _token = new byte[0];
        /// <summary>
        /// Gets or sets a opaque token used to correlate messages over multiple responses (i.e. when a reponse is not piggy-backed to the acknowledgement.
        /// This token may be any size up to 8 bytes long. When set to a zero length, it will not be serialised as part of the message.
        /// </summary>
        public byte[] Token
        {
            get => _token;
            set
            {
                if (value.Length > 8)
                    throw new ArgumentException("Token length can not be more than 8 bytes long");
                _token = value;
            }
        }

        /// <summary>
        /// Gets or Sets a Message ID to pair Requests to their immediate Responses.
        /// </summary>
        public ushort Id { get; set; }

        private List<Option> _options = new List<Option>();
        /// <summary>
        /// Gets or sets the list of options to be encoded into the message header. The order of these options are Critical and spcial care is needed when adding new items.
        /// <para>Todo: Sort items based on <see cref="Option.OptionNumber"/> and preserve options with identical Optionnumbers</para>
        /// /// <para>Todo: Throw exception when non-repeatable <see cref="Option"/>s are addedd</para>
        /// </summary>
        public List<Option> Options
        {
            get { return _options; }
            set { _options = value; }
        }

        /// <summary>
        /// Gets or Sets The paylaod of the message.
        /// </summary>
        /// <remarks>Check (or add) <see cref="Options.ContentFormat"/> in <see cref="Message.Options"/> for the format of the payload.</remarks>
        public byte[] Payload { get; set; }

        public Message() { }

        /// <summary>
        /// Serialises the message into bytes, ready to be encrypted or transported to the destination endpoint.
        /// </summary>
        /// <returns></returns>
        public byte[] Serialise()
        {
            var result = new List<byte>();
            byte optCode = 0;

            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            // |Ver| T |  TKL  |      Code     |           Message ID          |
            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            var type = (byte)Type;
            result.Add((byte)(0x40 | ((type << 4) & 0x30) | _token.Length)); // Ver | T | TKL

            // +-+-+-+-+-+-+-+-+
            // |class|  detail | (See section 5.2 of [RFC7252])
            // +-+-+-+-+-+-+-+-+
            optCode = (byte)(((int)Code / 100) << 5); // Class
            optCode |= (byte)((int)Code % 100);       // Detail
            result.Add(optCode); // Code

            result.Add((byte)((Id >> 8) & 0xFF)); // Message ID (upper byte)
            result.Add((byte)(Id & 0xFF));        // Message ID (lower byte)

            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            // | Token (if any, TKL bytes) ...
            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            result.AddRange(_token);

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
                result.Add(0xFF); // Payload marker
                result.AddRange(Payload);
            }

            return result.ToArray();
        }

        public void Deserialise(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (data.Length < 4)
                throw new ArgumentException("Message should be atleast 4 bytes long");
            if ((data[0] & 0xC0) != 0x40)
                throw new ArgumentException("Only verison 1 of CoAP protocl is supported");

            Type = (MessageType)((data[0] & 0x30) >> 4);
            Code = (MessageCode)data[1];
            Id = (ushort)((data[2] << 8) | (data[3]));

            if ((data[0] & 0x0F) > 0)
                _token = data.Skip(4).Take(data[0] & 0x0F).ToArray();


        }

        /// <summary>
        /// Shortcut method to create a <see cref="Message"/> with its optinos pre-populated to match the Uri.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Message CreateFromUri(string input)
        {
            var message = new Message();
            message.FromUri(input);
            return message;
        }

        /// <summary>
        /// Popualtes <see cref="Message.Options"/> to match the Uri.
        /// </summary>
        /// <remarks>Any potentially conflicting <see cref="Option"/>s are stripped after URI validation and before processing.</remarks>
        /// <param name="input"></param>
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
                    _options.Add(new Options.UriHost(uri.IdnHost));
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