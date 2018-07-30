using CoAPNet.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoAPNet
{
    public static class CoapMessageContextExtensions
    {
        public static CoapBlockWiseContext CreateBlockWiseContext(this CoapMessage message, CoapClient client, CoapMessage response = null)
        {
            if (!message.Code.IsRequest())
                throw new ArgumentException($"A block-Wise context requires a base request message. Message code {message.Code} is invalid.", nameof(message));

            if (response != null && response.Code.IsRequest())
                throw new ArgumentException($"A block-Wise context response can not be set from a message code {message.Code}.", nameof(response));

            return new CoapBlockWiseContext(client, message, response);
        }
    }

    public class CoapBlockWiseContext
    {
        public CoapClient Client { get; }

        public CoapMessage Request { get; internal set; }

        public CoapMessage Response { get; internal set; }

        public CoapMessageIdentifier MessageId { get; internal set; }

        public CoapBlockWiseContext(CoapClient client, CoapMessage request, CoapMessage response = null)
        {
            Client = client
                ?? throw new ArgumentNullException(nameof(client));

            Request = request?.Clone(true)
                ?? throw new ArgumentNullException(nameof(request));

            Response = response?.Clone(true);
        }
    }
}
