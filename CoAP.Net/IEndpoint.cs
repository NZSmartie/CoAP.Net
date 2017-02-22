using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoAP.Net
{
    public class CoapPayload
    {
        public virtual int MessageId { get; set; }

        public virtual byte[] Payload { get; set; }

        public virtual ICoapEndpoint Endpoint { get; set; }
    }

    // Provided by Application  Layer
    public interface ICoapEndpoint
    {
        /// <summary>
        /// Called by [Service] to send a <see cref="CoapPayload.Payload"/> to the specified <see cref="CoapPayload.Endpoint"/> using the transport layer provided by the Application Layer
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        Task SendAsync(CoapPayload payload);

        /// <summary>
        /// Called by [service] to receive data from the transport layer
        /// </summary>
        /// <returns></returns>
        Task<CoapPayload> ReceiveAsync();
    }
}
