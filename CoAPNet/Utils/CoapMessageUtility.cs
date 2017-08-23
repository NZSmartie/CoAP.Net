using System;
using System.Text;
using CoAPNet.Options;

namespace CoAPNet.Utils
{
    public static class CoapMessageUtility
    {
        public static CoapMessage CreateMessage(CoapMessageCode code, string message, CoapMessageType type = CoapMessageType.Confirmable)
        {
            return new CoapMessage
            {
                Code = code,
                Type = type,
                Options = {new ContentFormat(ContentFormatType.TextPlain)},
                Payload = Encoding.UTF8.GetBytes(message)
            };
        }

        public static CoapMessage FromException(Exception exception)
        {
            var result = new CoapMessage
            {
                Type = CoapMessageType.Reset,
                Code = CoapMessageCode.InternalServerError,
                Options = {new ContentFormat(ContentFormatType.TextPlain)},
                Payload = Encoding.UTF8.GetBytes(exception.Message)
            };

            switch (exception)
            {
                case CoapOptionException _:
                    result.Code = CoapMessageCode.BadOption;
                    break;
                case NotImplementedException _:
                    result.Code = CoapMessageCode.NotImplemented;
                    break;
            }
            return result;
        }
    }
}