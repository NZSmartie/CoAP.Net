using System;
using System.Text;
using CoAPNet.Options;

namespace CoAPNet.Utils
{
    public static class CoapMessageUtility
    {
        public static CoapMessage CreateMessage(CoapMessageCode code, string message)
        {
            return new CoapMessage
            {
                Code = code,
                Options = {new ContentFormat(ContentFormatType.TextPlain)},
                Payload = Encoding.UTF8.GetBytes(message)
            };
        }

        public static CoapMessage FromException(Exception exception)
        {
            return new CoapMessage
            {
                Code = CoapMessageCode.InternalServerError,
                Options = { new ContentFormat(ContentFormatType.TextPlain) },
                Payload = Encoding.UTF8.GetBytes(exception.Message)
            };
        }
    }
}