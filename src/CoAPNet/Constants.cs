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

namespace CoAPNet
{
    public static class CoapRegisteredOptionNumber
    {
        public const int IfMatch = 1;
        public const int UriHost = 3;
        public const int ETag = 4;
        public const int IfNoneMatch = 5;
        public const int UriPort = 7;
        public const int LocationPath = 8;
        public const int UriPath = 11;
        public const int ContentFormat = 12;
        public const int MaxAge = 14;
        public const int UriQuery = 15;
        public const int Accept = 17;
        public const int LocationQuery = 20;
        public const int Block1 = 27;
        public const int Block2 = 23;
        public const int ProxyUri = 35;
        public const int ProxyScheme = 39;
        public const int Size1 = 60;
        public const int Size2 = 28;
    }

    /// <summary>
    /// Constants and Defaults derrived from RFC 7252
    /// </summary>
    public static class Coap
    {
        /// <summary>
        /// IPv4 muilticast address as registered with IANA
        /// </summary>
        public const string MulticastIPv4 = "224.0.1.187";

        /// <summary>
        /// IPv6 multicast address where X is replaced with the desired scope. 
        /// <para>See <see cref="GetMulticastIPv6ForScope(int)"/> for getting the IPv6 multicast address for a desired scope.</para>
        /// </summary>
        public const string MulticastIPv6 = "FF0X::FD";

        /// <summary>
        /// The default UDP port for <c>coap://</c> schema
        /// </summary>
        public const ushort Port = 5683;

        /// <summary>
        /// The default UDP port for <c>coaps://</c> schema for secure connections over DTLS
        /// </summary>
        public const ushort PortDTLS = 5684;

        /// <summary>
        /// Default amount of re-transmission attempts when sending a message from <see cref="CoapClient"/>
        /// </summary>
        public const int MaxRestransmitAttempts = 3;

        /// <summary>
        /// Default period between transmission attempts when sending a message from <see cref="CoapClient"/>
        /// </summary>
        public static readonly TimeSpan RetransmitTimeout = TimeSpan.FromSeconds(2);

        /// <summary>
        /// IPv6 multicast address for a specific IPv6 scope.
        /// </summary>
        /// <param name="scope">The IPv6 scope in numberic representation.</param>
        /// <returns>The IPv6 multicast address for the specifiec scope.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Scope must be in the range from 1 to 14. 0 and 15 are reserved. (See RFC 7346)</exception>
        public static string GetMulticastIPv6ForScope(int scope)
        {
            if(scope < 1 || scope >= 15)
                throw new ArgumentOutOfRangeException(nameof(scope), "Scope must be in the range from 1 to 14. 0 and 15 are reserved. (See RFC 7346)");
            return MulticastIPv6.Replace("X", scope.ToString("X"));
        }

        public static readonly int[] ReservedMessageCodeClasses = new int[] { 1, 6, 7 };
    }
}
