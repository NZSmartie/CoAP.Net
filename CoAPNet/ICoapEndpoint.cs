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
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace CoAPNet
{
    [ExcludeFromCodeCoverage]
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
        Task SendAsync(CoapPacket packet);

        /// <summary>
        /// Called by [service] to receive data from the endpoint layer
        /// </summary>
        /// <returns></returns>
        Task<CoapPacket> ReceiveAsync();
    }

    /// <summary>
    /// Will be used as a place holder for endpoints without a known implementation
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CoapEndpoint : ICoapEndpoint
    {
        public void Dispose()
        { }

        public bool IsSecure { get; internal set; }
        public bool IsMulticast { get; internal set; }
        public Uri BaseUri { get; internal set; }
        public Task SendAsync(CoapPacket packet)
        {
            throw new InvalidOperationException($"{nameof(CoapEndpoint)} can not be used to send and receive");
        }

        public Task<CoapPacket> ReceiveAsync()
        {
            throw new InvalidOperationException($"{nameof(CoapEndpoint)} can not be used to send and receive");
        }
    }
}