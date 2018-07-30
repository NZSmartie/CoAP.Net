using System;
using System.Text;
using CoAPNet.Options;

namespace CoAPNet
{
    public partial class CoapMessage
    {
        /// <summary>
        /// Reads in binary data that forms a CoAP message and creates a new <see cref="CoapMessage"/>
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="isMulticast">Indicates if this message was received from a multicast endpoint.</param>
        /// <returns></returns>
        public static CoapMessage CreateFromBytes(in byte[] payload, bool isMulticast = false)
        {
            var message = new CoapMessage(isMulticast);
            message.FromBytes(payload);
            return message;
        }

        /// <summary>
        /// Create a new <c>text/plain</c> CoAP message
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static CoapMessage Create(CoapMessageCode code, string message, CoapMessageType type = CoapMessageType.Confirmable)
        {
            return new CoapMessage
            {
                Code = code,
                Type = type,
                Options = { new ContentFormat(ContentFormatType.TextPlain) },
                Payload = Encoding.UTF8.GetBytes(message)
            };
        }

        /// <summary>
        /// Create a new <see cref="CoapMessage"/> with its optinos pre-populated to match <paramref name="input"/>.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static CoapMessage CreateFromUri(string input)
        {
            var message = new CoapMessage();
            message.SetUri(input);
            return message;
        }

        /// <summary>
        /// Create a new CoAP messages with <see cref="CoapMessageCode"/> set based on the type of <see cref="Exception"/> provided.
        /// </summary>
        /// <remarks>
        /// <see cref="CoapMessage"/>.<see cref="Code"/> will be set to <see cref="CoapMessageCode.InternalServerError"/> by default unless:
        /// <list type="bullet">
        ///   <item>
        ///     <description>When <paramref name="exception"/> is of type <see cref="CoapException"/>. Then <see cref="CoapMessage"/>.<see cref="Code"/> will be set to <see cref="CoapException"/>.<see cref="CoapException.ResponseCode"/></description>
        ///   </item>
        ///   <item>
        ///     <description>When <paramref name="exception"/> is of type <see cref="NotImplementedException"/>. Then <see cref="CoapMessage"/>.<see cref="Code"/> will be set to <see cref="CoapMessageCode.NotImplemented"/></description>
        ///   </item>
        /// </list>
        /// </remarks>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static CoapMessage FromException(Exception exception)
        {
            var result = new CoapMessage
            {
                Type = CoapMessageType.Reset,
                Code = CoapMessageCode.InternalServerError,
                Options = { new ContentFormat(ContentFormatType.TextPlain) },
                Payload = Encoding.UTF8.GetBytes(exception.Message)
            };

            switch (exception)
            {
                case CoapException coapEx:
                    result.Code = coapEx.ResponseCode;
                    break;
                case NotImplementedException _:
                    result.Code = CoapMessageCode.NotImplemented;
                    break;
            }
            return result;
        }

        /// <summary>
        /// Returns a new deep-clone of this <see cref="CoapMessage"/>.
        /// </summary>
        /// <returns></returns>
        public CoapMessage Clone(bool withPayload = false)
        {
            return new CoapMessage
            {
                _version = _version, // future proofing just in case?
                Id = Id,
                Type = Type,
                Code = Code,
                _token = _token.Clone() as byte[],
                _options = new System.Collections.Generic.List<CoapOption>(_options),
                IsMulticast = IsMulticast,
                Payload = withPayload ? Payload?.Clone() as byte[] : null,
            };
        }
    }
}
