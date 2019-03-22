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
using System.IO;
using System.Linq;
using System.Text;
using CoAPNet.Options;

namespace CoAPNet
{
    /// <summary>
    /// See <see cref="CoapMessage"/>.<see cref="CoapMessage.Type"/>
    /// </summary>
    public enum CoapMessageType
    {
        /// <summary>
        /// Marks a <see cref="CoapMessage"/> as confirmable (CON) which must be acknowleged (ACK) or reset (RST) from the recipiant.
        /// </summary>
        Confirmable = 0,
        /// <summary>
        /// Marks a <see cref="CoapMessage"/> as non-confirmable (NON) which may be ignored by the recipiant or safely lost during transit.
        /// </summary>
        NonConfirmable = 1,
        /// <summary>
        /// Marks a <see cref="CoapMessage"/> as an acknowledgement (ACK) to a previous confirmable (CON) <see cref="CoapMessage"/>.
        /// </summary>
        Acknowledgement = 2,
        /// <summary>
        /// Marks a <see cref="CoapMessage"/> as a reset (RST) to a previous <see cref="CoapMessage"/> that was received in error or was invalid.
        /// </summary>
        Reset = 3,
    }

    /// <summary>
    /// Class pages used to indicate if a <see cref="CoapMessageCode"/> value is a Request, or a Response or an error.
    /// </summary>
    public enum CoapMessageCodeClass
    {
        /// <summary>
        /// Classifies <see cref="CoapMessage"/> as a request to a remote endpoint.
        /// </summary>
        Request = 0,
        /// <summary>
        /// Classifies <see cref="CoapMessage"/> as a successful response from a remote endpoint.
        /// </summary>
        Success = 2,
        /// <summary>
        /// Classifies <see cref="CoapMessage"/> as an error response to a prior request that was invalid.
        /// </summary>
        ClientError = 4,
        /// <summary>
        /// Classifies <see cref="CoapMessage"/> as an error response to a prior request that caused the remote to have an internal error.
        /// </summary>
        ServerError = 5
    }

    /// <summary>
    /// Represents CoAP errors that arise during parsing or serialising operations.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CoapMessageFormatException : CoapException {

        /// <inheritdoc/>
        public CoapMessageFormatException() :base() { }

        /// <inheritdoc/>
        public CoapMessageFormatException(string message) : base(message) { }

        /// <inheritdoc/>
        public CoapMessageFormatException(string message, CoapMessageCode responseCode) : base(message, responseCode) { }

        /// <inheritdoc/>
        public CoapMessageFormatException(string message, Exception innerException) : base(message, innerException) { }

        /// <inheritdoc/>
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
        public CoapMessageCode Code { get; set; } = CoapMessageCode.None;

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
                _options = value.OrderBy(o => o.OptionNumber).ToList();
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
        [Obsolete]
        public byte[] ToBytes()
        {
            using (var ms = new MemoryStream())
            {
                Encode(ms);
                return ms.ToArray();
            }
        }

        public void Encode(Stream stream)
        {
            byte optCode = 0;

            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            // |Ver| T |  TKL  |      Code     |           Message ID          |
            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            var type = (byte)Type;
            stream.WriteByte((byte)(0x40 | ((type << 4) & 0x30) | (Code == CoapMessageCode.None ? 0 : _token.Length))); // Ver | T | TKL

            // +-+-+-+-+-+-+-+-+
            // |class|  detail | (See section 5.2 of [RFC7252])
            // +-+-+-+-+-+-+-+-+
            optCode = (byte)(Code.Class << 5); // Class
            optCode |= (byte)Code.Detail;      // Detail
            stream.WriteByte(optCode); // Code

            stream.WriteByte((byte)((Id >> 8) & 0xFF)); // Message ID (upper byte)
            stream.WriteByte((byte)(Id & 0xFF));        // Message ID (lower byte)

            // Empty messages must only contain a 4 byte header.
            if (Code == CoapMessageCode.None)
                return;

            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            // | Token (if any, TKL bytes) ...
            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            stream.Write(_token, 0, _token.Length);

            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            // | Options (if any) ...
            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            var currentOptionDelta = 0;

            foreach (var option in _options.OrderBy(o => o.OptionNumber))
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
                    stream.WriteByte((byte)(optCode | 0x0E));
                    optionDelta -= 269;

                    stream.Write(optionHeader.ToArray(), 0, optionHeader.Count);
                    stream.WriteByte((byte)((optionDelta & 0xFF00u) >> 8));
                    stream.WriteByte((byte)(optionDelta & 0xFFu));
                }
                else if (optionDelta >= 13)
                {
                    stream.WriteByte((byte)(optCode | 0x0D));
                    optionDelta -= 13;

                    stream.Write(optionHeader.ToArray(), 0, optionHeader.Count);
                    stream.WriteByte((byte)(optionDelta & 0xFFu));
                }
                else
                {
                    stream.WriteByte((byte)(optCode | optionDelta));
                    stream.Write(optionHeader.ToArray(), 0, optionHeader.Count);
                }

                option.Encode(stream);
            }

            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            // |1 1 1 1 1 1 1 1| Payload (if any) ...
            // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

            if (Payload != null && Payload.Length > 0)
            {
                // TODO: Write a Pyload stream, do not hold a byte array.
                stream.WriteByte(0xFF); // Payload marker
                stream.Write(Payload, 0, Payload.Length);
            }
        }

        /// <summary>
        /// Deserialises a <see cref="byte"/>[] to a <see cref="CoapMessage"/>.
        /// </summary>
        /// <remarks>
        /// The header and options will be read as much as possible to help diagnose problems when an <see cref="CoapException"/> is thrown.
        /// </remarks>
        /// <param name="input"></param>
        [Obsolete]
        public void FromBytes(in byte[] input)
        {
            using (var ms = new MemoryStream(input))
                Decode(ms);
        }

        public void Decode(Stream stream)
        {
            //if (data.Length < 4)
            //    throw new CoapMessageFormatException("Message must be at least 4 bytes long");

            var type = stream.ReadByte();

            if ((type & 0xC0) != 0x40)
                throw new CoapMessageFormatException($"Unexpected CoAP version ({type & 0xC0:D}). Only verison 1 is supported");

            // Allocate a byte array for the token
            _token = new byte[type & 0x0F];

            Type = (CoapMessageType)((type & 0x30) >> 4);

            type = stream.ReadByte();
            Code = new CoapMessageCode(((type & 0xE0) >> 5), type & 0x1F);

            Id = (ushort)((stream.ReadByte() << 8) | (stream.ReadByte()));

            // Don't process any further if this is a "empty" message
            if (Code == CoapMessageCode.None && stream.ReadByte() != -1)
                throw new CoapMessageFormatException("Empty message must be 4 bytes long");

            if (_token.Length > 0)
                stream.Read(_token, 0, _token.Length);

            // Catch all the CoapOptionExceptions and throw them after all the options have been parsed.
            var badOptions = new List<CoapOptionException>();
            var optionDelta = 0;
            do
            {
                type = stream.ReadByte();

                // check for payload marker
                if (type == 0xFF || type < 0)
                    break;

                var optCode = (type & 0xF0) >> 4;
                var dataLen = (type & 0x0F);

                if (optCode == 13)
                {
                    optCode = stream.ReadByte() + 13;
                }
                else if (optCode == 14)
                {
                    optCode = stream.ReadByte() << 8;
                    optCode |= stream.ReadByte();
                    optCode += 269;

                }
                if (dataLen == 13)
                {

                    dataLen = stream.ReadByte() + 13;
                }
                else if (dataLen == 14)
                {
                    dataLen = stream.ReadByte() << 8;
                    dataLen |= stream.ReadByte();
                    dataLen += 269;
                }

                try
                {
                    var option = OptionFactory.Create(optCode + optionDelta, stream, dataLen);
                    if (option != null)
                        Options.Add(option);
                }
                catch (CoapOptionException ex)
                {
                    badOptions.Add(ex);
                }

                optionDelta += optCode;
            } while (stream.CanRead);

            // Performing this check after parsing the options to allow the chance of reading the message token
            if (Coap.ReservedMessageCodeClasses.Contains(Code.Class))
                throw new CoapMessageFormatException("Message.Code can not use reserved classes");

            if (badOptions.Count == 1)
                throw badOptions.First();

            if (badOptions.Count > 1)
                throw new AggregateException(badOptions);

            // TODO: remove this step?
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                Payload = ms.ToArray();
            }
            
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
            var uri = new UriBuilder
            {
                Scheme = "coap",
                Host = _options.Get<Options.UriHost>()?.ValueString ?? "localhost"
            };

            var port = _options.Get<Options.UriPort>()?.ValueUInt ?? Coap.Port;
            if (port != Coap.Port)
            {
                if (port == Coap.PortDTLS)
                    uri.Scheme = "coaps";
                else
                    uri.Port = (int)port;
            }

            uri.Path = $"/{string.Join("/", _options.GetAll<UriPath>().Select(p => p.ValueString))}";

            if(_options.Contains<Options.UriQuery>())
                uri.Query = $"?{string.Join(" & ", _options.GetAll<UriQuery>().Select(q => q.ValueString))}";

            return uri.Uri;
        }

        /// <summary>
        /// For debug purposes mostly: Represents a CoAP Message in human readable form.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var result = new StringBuilder(Type == CoapMessageType.Acknowledgement ? "ACK" :
                                           Type == CoapMessageType.Confirmable     ? "CON" :
                                           Type == CoapMessageType.NonConfirmable  ? "NON" : "RST");

            result.Append($", MID:{Id}, {Code}");

            if (Options.Any(o => o.OptionNumber == CoapRegisteredOptionNumber.UriPath))
                result.Append(", /").Append(Options.Where(o => o.OptionNumber == CoapRegisteredOptionNumber.UriPath).Select(o => o.ValueString).Aggregate((a, b) => a + "/" + b));

            // TODO: These lines shouldn't be here. Somehow extend CoapMEssage.ToString()?
            var block1 = Options.Get<Options.Block1>();
            if (block1 != null)
                result.Append($", 1:{block1.BlockNumber}/{(block1.IsMoreFollowing ? "1" : "0")}/{block1.BlockSize}");

            // TODO: These lines shouldn't be here. Somehow extend CoapMEssage.ToString()?
            var block2 = Options.Get<Options.Block2>();
            if (block2 != null)
                result.Append($", 2:{block2.BlockNumber}/{(block2.IsMoreFollowing ? "1" : "0")}/{block2.BlockSize}");

            return result.ToString();
        }
    }
}
