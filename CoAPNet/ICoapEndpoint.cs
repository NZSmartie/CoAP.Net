using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoAPNet
{
    public class CoapEndpointException : Exception
    {
        public CoapEndpointException() : base() { }

        public CoapEndpointException(string message) : base(message) { }

        public CoapEndpointException(string message, Exception innerException) : base(message, innerException) { }
    }

    public interface ICoapConnectionInformation
    {
        ICoapEndpoint LocalEndpoint { get; }

        ICoapEndpoint RemoteEndpoint { get; }
    }

    public interface ICoapEndpoint : IDisposable
    {
        /// <summary>
        /// Gets if this enpoint is encrypted using DTLS
        /// </summary>
        bool IsSecure { get; }

        /// <summary>
        /// Gets if this enpoint used for Multicast.
        /// </summary>
        /// <remarks>
        /// Multicast endpoitns do not acknolweged received confirmables.
        /// </remarks>
        bool IsMulticast { get; }

        /// <summary>
        /// Gets the base URI (excluding path and query) for this endpoint. 
        /// </summary>
        Uri BaseUri { get; }

        /// <summary>
        /// Called by [Service] to send a <see cref="CoapPacket.Payload"/> to the specified <see cref="CoapPacket.Endpoint"/> using the endpoint layer provided by the Application Layer
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task SendAsync(CoapPacket packet, CancellationToken token);

        /// <summary>
        /// Called by [service] to receive data from the endpoint layer
        /// </summary>
        /// <returns></returns>
        Task<CoapPacket> ReceiveAsync(CancellationToken token);
    }

    /// <summary>
    /// Will be used as a place holder for endpoints without a known implementation
    /// </summary>
    public class CoapEndpoint : ICoapEndpoint
    {
        public void Dispose()
        { }

        public bool IsSecure { get; internal set; }
        public bool IsMulticast { get; internal set; }
        public Uri BaseUri { get; internal set; }
        public Task SendAsync(CoapPacket packet, CancellationToken token)
        {
            throw new InvalidOperationException($"{nameof(CoapEndpoint)} can not be used to send and receive");
        }

        public Task<CoapPacket> ReceiveAsync(CancellationToken token)
        {
            throw new InvalidOperationException($"{nameof(CoapEndpoint)} can not be used to send and receive");
        }
    }
}