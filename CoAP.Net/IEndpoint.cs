using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoAP.Net
{
    public interface ICoapPayload
    {
        int MessageId { get; set; }

        byte[] Payload { get; set; }

        ICoapEndpoint Endpoint { get; set; }
    }

    public interface ICoapEndpoint
    {
        Task SendAsync(byte[] data);

        Task<ICoapPayload> ReceiveAsync();
    }
}
