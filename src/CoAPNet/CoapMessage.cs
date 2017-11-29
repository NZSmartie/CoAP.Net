#region License
// Copyright 2017 Roman Vaughan (NZSmartie)
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CoAPNet.Options;

namespace CoAPNet
{
    /// <summary>
    /// See <see cref="CoapMessage"/>.<see cref="CoapMessage.Type"/>
    /// </summary>
    public enum CoapMessageType
    {
        Confirmable = 0,
        NonConfirmable = 1,
        Acknowledgement = 2,
        Reset = 3,
    }

    /// <summary>
    /// Class pages used to indicate if a <see cref="CoapMessageCode"/> value is a Request, or a Response or an error.
    /// </summary>
    public enum CoapMessageCodeClass
    {
        Request = 0,
        Success = 2,
        ClientError = 4,
        ServerError = 5
    }

    /// <summary>
    /// Represents CoAP errors that arise during parsing or serialising operations.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CoapMessageFormatException : CoapException {

        public CoapMessageFormatException() :base() { }

        public CoapMessageFormatException(string message) : base(message) { }

        public CoapMessageFormatException(string message, CoapMessageCode responseCode) : base(message, responseCode) { }

        public CoapMessageFormatException(string message, Exception innerException) : base(message, innerException) { }

        public CoapMessageFormatException(string message, Exception innerException, CoapMessageCode responseCode) : base(message, innerException, responseCode) { }
    }

    /// <summary>
    /// An object to represent an CoAP message received or to be sent.
    /// </summary>
    public partial class CoapMessage
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
        /// <list type="bullet">
        ///   <item>
        ///     <description>When set to <see cref="CoapMessageType.Reset"/>, this message indicates that a <see cref="CoapMessageType.Confirmable"/> message was rejected by the server endpoint.</description>
        ///   </item>
        ///   <item>
        ///     <description>When set to <see cref="CoapMessageType.Acknowledgement"/>, this message indicates it was accepted (and responded) by the server enpoint.</description>
        ///   </item>
        /// </list>
        /// </summary>
        public CoapMessageType Type { get; set; }

        /// <summary>
        /// Gets or Sets the Message Code. 
        /// <para>The class indicates if the message is <see cref="CoapMessageCodeClass.Request"/>, <see cref="CoapMessageCodeClass.Success"/>, <see cref="CoapMessageCodeClass.ClientError"/>, or a <see cref="CoapMessageCodeClass.ServerError"/></para>
        /// <para>See section 2.2 of [RFC7252]</para>
        /// </summary>
        public CoapMessageCode Code { get; set; }

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
        public int Id { get; set; }

        private List<CoapOption> _options = new List<CoapOption>();

        /// <summary>
        /// Gets or sets the list of options to be encoded into the message header. The order of these options are Critical and spcial care is needed when adding new items.
        /// <para>Todo: Sort items based on <see cref="CoapOption.OptionNumber"/> and preserve options with identical Optionnumbers</para>
        /// /// <para>Todo: Throw exception when non-repeatable <see cref="CoapOption"/>s are addedd</para>
        /// </summary>
        public List<CoapOption> Options
        {
            get { return _options; }
            set
            {
                _options = value;
                _options.Sort();
            }
        }

        private OptionFactory _optionFactory;

        /// <summary>
        /// Gets or Sets the OptionFactory used when decoding options in a CoAP message header
        /// </summary>
        public OptionFactory OptionFactory
        {
            get => _optionFactory ?? (_optionFactory = OptionFactory.Default);
            set => _optionFactory = value;
        }

        /// <summary>
        /// Gets or Sets The paylaod of the message.
        /// </summary>
        /// <remarks>Check (or add) <see cref="ContentFormat"/> in <see cref="CoapMessage.Options"/> for the format of the payload.</remarks>
        public byte[] Payload { get; set; }

        /// <summary>
        /// Indicates if this CoAP message is received from a multicast endpoint or intended to be sent to a multicast endpoint
        /// </summary>
        public bool IsMulticast { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="multicast"></param>
        public CoapMessage(bool multicast = false)
        {
            IsMulticast = multicast;
        }

        /// <summary>
        /// Serialises the message into bytes, ready to be encrypted or transported to the destination endpoint.
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
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

            // Empty messages must only contain a 4 byte header.
            if (Code == CoapMessageCode.None)
            {
                result[0] &= 0xF0; // Zero out the token length in case the application layer set one
                return result.ToArray();
            }

            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            // | Token (if any, TKL bytes) ...
            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            result.AddRange(_token);

            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            // | Options (if any) ...
            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            var currentOptionDelta = 0;

            _options.Sort();
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

        public void FromBytes(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length < 4)
                throw new CoapMessageFormatException("Message must be at least 4 bytes long");
            if ((data[0] & 0xC0) != 0x40)
                throw new CoapMessageFormatException($"Unexpected CoAP version ({data[0] & 0xC0:D}). Only verison 1 is supported");

            var offset = 4;

            Type = (CoapMessageType)((data[0] & 0x30) >> 4);

            var code = ((data[1] & 0xE0) >> 5) * 100;
            code += data[1] & 0x1F;
            Code = (CoapMessageCode)code;

            Id = (ushort)((data[2] << 8) | (data[3]));

            // Don't process any further if this is a "empty" message
            if (Code == CoapMessageCode.None && data.Length > 4)
                throw new CoapMessageFormatException("Empty message must be 4 bytes long");

            offset += data[0] & 0x0F;
            if ((data[0] & 0x0F) > 0)
                _token = data.Skip(4).Take(data[0] & 0x0F).ToArray();

            // Catch all the CoapOptionExceptions and throw them after all the options have been parsed.
            var badOptions = new List<CoapOptionException>();
            var optionDelta = 0;
            for(var i = offset; i<data.Length; i++)
            {
                // check for payload marker
                if (data[i] == 0xFF)
                {
                    Payload = data.Skip(i + 1).ToArray();
                    break;
                }

                var optCode = (data[i] & 0xF0) >> 4;
                var dataLen = (data[i] & 0x0F);

                if (optCode == 13)
                    optCode = data[i++ + 1] + 13;
                else if (optCode == 14)
                {
                    optCode = data[i++ + 1] << 8;
                    optCode |= data[i++ + 1] + 269;
                    
                }
                if (dataLen == 13)
                    dataLen = data[i++ + 1];
                else if (dataLen == 14)
                {
                    dataLen = data[i++ + 1] << 8;
                    dataLen |= data[i++ + 1] + 269;
                }

                try
                {
                    var option = OptionFactory.Create(optCode + optionDelta,
                        data.Skip(i + 1).Take(dataLen).ToArray());
                    if (option != null)
                        Options.Add(option);
                }
                catch (CoapOptionException ex)
                {
                    badOptions.Add(ex);
                }

                i += dataLen;
                optionDelta += optCode;
            }

            // Performing this check after parsing the options to allow the chance of reading the message token
            if (new int[] {1, 6, 7}.Contains(code / 100))
                throw new CoapMessageFormatException("Message.Code can not use reserved classes");

            if (badOptions.Count == 1)
                throw badOptions.First();

            if (badOptions.Count > 1)
                throw new AggregateException(badOptions);
        }

        /// <summary>
        /// Obsolete: See <see cref="SetUri(string)"/>
        /// </summary>
        /// <param name="input"></param>
        [Obsolete]
        public void FromUri(string input)
            => SetUri(new Uri(input, UriKind.RelativeOrAbsolute));

        /// <summary>
        /// Obsolete: See <see cref="SetUri(Uri)"/>
        /// </summary>
        [Obsolete]
        public void FromUri(Uri uri)
            => SetUri(uri);

        /// <summary>
        /// Popualtes <see cref="Options"/> to match the Uri.
        /// </summary>
        /// <remarks>Any potentially conflicting <see cref="CoapOption"/>s are stripped after URI validation and before processing.</remarks>
        /// <param name="input"></param>
        /// <param name="parts"></param>
        public void SetUri(string input, UriComponents parts = UriComponents.HttpRequestUrl)
            => SetUri(new Uri(input, UriKind.RelativeOrAbsolute), parts);

        /// <summary>
        /// Popualtes <see cref="Options"/> to match the Uri.
        /// </summary>
        /// <remarks>Any potentially conflicting <see cref="CoapOption"/>s are stripped after URI validation and before processing.</remarks>
        /// <param name="uri"></param>
        /// <param name="parts"></param>
        public void SetUri(Uri uri, UriComponents parts = UriComponents.HttpRequestUrl) { 

            //if (!uri.IsAbsoluteUri)
            //    throw new UriFormatException("URI is not absolute and unsupported by the CoAP scheme");

            if (parts.HasFlag(UriComponents.Scheme) && uri.Scheme != "coap" && uri.Scheme != "coaps")
                throw new UriFormatException("Input URI scheme is not coap:// or coaps://");

            if (parts.HasFlag(UriComponents.Fragment) && uri.Fragment.Length > 0)
                throw new UriFormatException("Fragments are unsupported in the CoAP scheme");

            // Strip out any existing URI Options 
            var optionsToDiscard = new List<int>();
            if (parts.HasFlag(UriComponents.Host))
                optionsToDiscard.Add(CoapRegisteredOptionNumber.UriHost);
            if (parts.HasFlag(UriComponents.Port))
                optionsToDiscard.Add(CoapRegisteredOptionNumber.UriPort);
            if (parts.HasFlag(UriComponents.Path))
                optionsToDiscard.Add(CoapRegisteredOptionNumber.UriPath);
            if (parts.HasFlag(UriComponents.Query))
                optionsToDiscard.Add(CoapRegisteredOptionNumber.UriQuery);

            _options = _options.Where(kv => !optionsToDiscard.Contains(kv.OptionNumber)).ToList();

            if (parts.HasFlag(UriComponents.Host))
            {
                switch (uri.HostNameType)
                {
                    case UriHostNameType.Dns:
                        _options.Add(new Options.UriHost(uri.IdnHost));
                        break;
                    case UriHostNameType.IPv4:
                    case UriHostNameType.IPv6:
                        _options.Add(new Options.UriHost(uri.Host));
                        break;
                    default:
                        throw new UriFormatException("Unknown Hostname");
                }
            }

            if (parts.HasFlag(UriComponents.Port))
            {

                if ((uri.Scheme == "coap" && !uri.IsDefaultPort && uri.Port != 5683) ||
                (uri.Scheme == "coaps" && !uri.IsDefaultPort && uri.Port != 5684))
                    _options.Add(new Options.UriPort((ushort)uri.Port));
            }

            // Can't access path parameters if uri is not absolute....
            if (!uri.IsAbsoluteUri)
                uri = new Uri(new Uri("coap://localhost/"), uri);

            if (parts.HasFlag(UriComponents.Path))
                _options.AddRange(uri.AbsolutePath.Substring(1).Split(new[] { '/' }).Select(p => new Options.UriPath(Uri.UnescapeDataString(p))));

            if (parts.HasFlag(UriComponents.Query) && uri.Query.Length > 0)
                _options.AddRange(uri.Query.Substring(1).Split(new[] { '&' }).Select(p => new Options.UriQuery(Uri.UnescapeDataString(p))));
        }

        /// <summary>
        /// Generates an <see cref="Uri"/> based on the uri-sub-classed <see cref="CoapOption"/> in <see cref="Options"/>
        /// </summary>
        /// <returns></returns>
        public Uri GetUri()
        {
            var uri = new UriBuilder();

            uri.Scheme = "coap";
            uri.Host = _options.Get<Options.UriHost>()?.ValueString ?? "localhost";

            var port = _options.Get<Options.UriPort>()?.ValueUInt ?? Coap.Port;
            if (port != Coap.Port)
            {
                if (port == Coap.PortDTLS)
                    uri.Scheme = "coaps";
                else
                    uri.Port = (int)port;
            }

            uri.Path = "/" + string.Join("/", _options.GetAll<Options.UriPath>().Select(p => p.ValueString));

            if(_options.Contains<Options.UriQuery>())
                uri.Query = "?" + string.Join("&", _options.GetAll<Options.UriQuery>().Select(q => q.ValueString));

            return uri.Uri;
        }

        /// <summary>
        /// For debug purposes mostly: Represents a CoAP Message in human readable form.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var result = Type == CoapMessageType.Acknowledgement ? "ACK" :
                         Type == CoapMessageType.Confirmable     ? "CON" :
                         Type == CoapMessageType.NonConfirmable  ? "NON" : "RST";

            result += ", MID:" + Id.ToString();

            if (Code <= CoapMessageCode.Delete)
            {
                result += ", " + Code.ToString();
            }
            else
            {
                result += string.Format(", {0}.{1:D2} {2}", ((int)Code / 100), ((int)Code % 100), Code);
            }

            if (Options.Any(o => o.OptionNumber == CoapRegisteredOptionNumber.UriPath))
                result += ", /" + Options.Where(o => o.OptionNumber == CoapRegisteredOptionNumber.UriPath).Select(o => o.ValueString).Aggregate((a, b) => a + "/" + b);

            return result;
        }
    }
}
