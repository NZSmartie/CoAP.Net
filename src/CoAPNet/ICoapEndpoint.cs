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
    /// <summary/>
    [ExcludeFromCodeCoverage]
    public class CoapEndpointException : Exception
    {
        /// <summary/>
        public CoapEndpointException() : base() { }

        /// <summary/>
        public CoapEndpointException(string message) : base(message) { }

        /// <summary/>
        public CoapEndpointException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Contains the local <see cref="ICoapEndpoint"/> and the remote <see cref="ICoapEndpoint"/> that are part of the associate message request or reponse.
    /// </summary>
    public interface ICoapConnectionInformation
    {
        /// <summary>
        /// The Local <see cref="ICoapEndpoint"/> that represents the current connection.
        /// </summary>
        ICoapEndpoint LocalEndpoint { get; }

        /// <summary>
        /// The Remote <see cref="ICoapEndpoint"/> that represents the 3rd party.
        /// </summary>
        ICoapEndpoint RemoteEndpoint { get; }
    }

    /// <summary>
    /// Used with <see cref="ICoapEndpoint.ToString(CoapEndpointStringFormat)"/> to get a string representation of a <see cref="ICoapEndpoint"/>.
    /// </summary>
    public enum CoapEndpointStringFormat
    {
        /// <summary>
        /// Return a simple string format represeantion of <see cref="ICoapEndpoint"/> (usually in the form of &lt;address&gt;:&lt;port&gt;)
        /// </summary>
        Simple,
        /// <summary>
        /// Used to get string representation of a <see cref="ICoapEndpoint"/> for debugging purposes.
        /// </summary>
        Debuggable,
    }

    /// <summary>
    /// CoAP usses a <see cref="ICoapEndpoint"/> as a addressing mechanism for other CoAP clients and servers on a transport.
    /// </summary>
    public interface ICoapEndpoint : IDisposable
    {
        /// <summary>
        /// Gets if this enpoint is encrypted using (e.g. DTLS when the endpoint uses UDP)
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
        Task<CoapPacket> ReceiveAsync(CancellationToken tokens);

        /// <summary>
        /// Returns a string representation of the <see cref="ICoapEndpoint"/>.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        string ToString(CoapEndpointStringFormat format);
    }

    /// <summary>
    /// Will be used as a place holder for endpoints without a known implementation
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CoapEndpoint : ICoapEndpoint
    {
        /// <inheritdoc />
        public void Dispose()
        { }

        /// <inheritdoc />
        public bool IsSecure { get; internal set; }

        /// <inheritdoc />
        public bool IsMulticast { get; internal set; }

        /// <inheritdoc />
        public Uri BaseUri { get; internal set; }

        /// <inheritdoc />
        public Task SendAsync(CoapPacket packet, CancellationToken token)
        {
            throw new InvalidOperationException($"{nameof(CoapEndpoint)} can not be used to send and receive");
        }

        /// <inheritdoc />
        public Task<CoapPacket> ReceiveAsync(CancellationToken token)
        {
            throw new InvalidOperationException($"{nameof(CoapEndpoint)} can not be used to send and receive");
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if(obj is CoapEndpoint other)
            {
                if (!other.BaseUri.Equals(BaseUri))
                    return false;
                if (!other.IsMulticast.Equals(IsMulticast))
                    return false;
                if (!other.IsSecure.Equals(IsSecure))
                    return false;
                return true;
            }
            return base.Equals(obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return 1404189491;
        }


        /// <inheritdoc />
        public override string ToString()
         => ToString(CoapEndpointStringFormat.Simple);

        /// <inheritdoc />
        public string ToString(CoapEndpointStringFormat format)
        {
            if(format == CoapEndpointStringFormat.Simple)
                return $"{BaseUri.Host}{(BaseUri.IsDefaultPort ? "" : ":" + BaseUri.Port)}";
            if (format == CoapEndpointStringFormat.Debuggable)
                return $"[ {BaseUri.Host}{(BaseUri.IsDefaultPort ? "" : ":" + BaseUri.Port)} {(IsMulticast ? "(M) " : "")}{(IsSecure ? "(S) " : "")}]";

            throw new ArgumentException(nameof(format));
        }
    }
}