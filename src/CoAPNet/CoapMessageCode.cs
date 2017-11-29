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
    /// <summary>
    /// Response Codes
    /// <para>See section 5.9 of [RFC7252] and section 12.1 of [RFC7252]</para>
    /// </summary>
    public class CoapMessageCode
    {
        public int Class { get; }

        public int Detail { get; }

        public CoapMessageCode(int codeClass, int detail)
        {
            Class = codeClass;
            Detail = detail;
        }

        public CoapMessageCode(CoapMessageCodeClass codeClass, int detail)
        {
            Class = (int)codeClass;
            Detail = detail;
        }

        /// <summary>
        /// indicates if the CoAP message is a Request from a client.
        /// </summary>
        public bool IsRequest()
            => Class == 0 && Detail != 0;

        /// <summary>
        /// indicates if the CoAP message is a successful response from a server.
        /// </summary>
        public bool IsSuccess()
            => Class == 2;

        /// <summary>
        /// indicates if the CoAP message is a error due to a client's request.
        /// </summary>
        public bool IsClientError()
            => Class == 4;

        /// <summary>
        /// indicates if the CoAP message is a error due to internal server issues.
        /// </summary>
        public bool IsServerError()
            => Class == 5;

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var code = obj as CoapMessageCode;
            return code != null &&
                   Class == code.Class &&
                   Detail == code.Detail;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = 867851563;
            hashCode = hashCode * -1521134295 + Class.GetHashCode();
            hashCode = hashCode * -1521134295 + Detail.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(CoapMessageCode code, CoapMessageCodeClass codeClass)
        {
            return code.Class == (int)codeClass;
        }

        public static bool operator !=(CoapMessageCode code, CoapMessageCodeClass codeClass)
        {
            return code.Class != (int)codeClass;
        }

        public static bool operator ==(CoapMessageCode a, CoapMessageCode b)
        {
            if (a is null || b is null)
                return a is null && b is null;
            return a.Class == b.Class && a.Detail == b.Detail;
        }

        public static bool operator !=(CoapMessageCode a, CoapMessageCode b)
        {
            if(a is null || b is null)
                return !(a is null) || !(b is null);
            return a.Class != b.Class || a.Detail != b.Detail;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Class}.{Detail:D2}";
        }


        /// <summary>
        /// Placeholder and will throw <see cref="InvalidOperationException"/> when used.
        /// </summary>
        public static readonly CoapMessageCode None = new CoapMessageCode(CoapMessageCodeClass.Request, 0);

        /// <summary>
        /// GET request from a client to a server used for retreiving resources
        /// </summary>
        public static readonly CoapMessageCode Get = new CoapMessageCode(CoapMessageCodeClass.Request, 1);

        /// <summary>
        /// Post request from a client to a server used for creating or updating resources
        /// </summary>
        public static readonly CoapMessageCode Post = new CoapMessageCode(CoapMessageCodeClass.Request, 2);

        /// <summary>
        /// Put request from a client to a server used for updating resources
        /// </summary>
        public static readonly CoapMessageCode Put = new CoapMessageCode(CoapMessageCodeClass.Request, 3);

        /// <summary>
        /// DELETE request from a client to a server used to delete resources
        /// </summary>
        public static readonly CoapMessageCode Delete = new CoapMessageCode(CoapMessageCodeClass.Request, 4);

        // 2.xx Success
        public static readonly CoapMessageCode Created = new CoapMessageCode(CoapMessageCodeClass.Success, 1);
        public static readonly CoapMessageCode Deleted = new CoapMessageCode(CoapMessageCodeClass.Success, 2);
        public static readonly CoapMessageCode Valid = new CoapMessageCode(CoapMessageCodeClass.Success, 3);
        public static readonly CoapMessageCode Changed = new CoapMessageCode(CoapMessageCodeClass.Success, 4);
        public static readonly CoapMessageCode Content = new CoapMessageCode(CoapMessageCodeClass.Success, 5);
        public static readonly CoapMessageCode Continue = new CoapMessageCode(CoapMessageCodeClass.Success, 31);
        
        // 4.xx Client Error
        public static readonly CoapMessageCode BadRequest = new CoapMessageCode(CoapMessageCodeClass.ClientError, 0);
        public static readonly CoapMessageCode Unauthorized = new CoapMessageCode(CoapMessageCodeClass.ClientError, 1);
        public static readonly CoapMessageCode BadOption = new CoapMessageCode(CoapMessageCodeClass.ClientError, 2);
        public static readonly CoapMessageCode Forbidden = new CoapMessageCode(CoapMessageCodeClass.ClientError, 3);
        public static readonly CoapMessageCode NotFound = new CoapMessageCode(CoapMessageCodeClass.ClientError, 4);
        public static readonly CoapMessageCode MethodNotAllowed = new CoapMessageCode(CoapMessageCodeClass.ClientError, 5);
        public static readonly CoapMessageCode NotAcceptable = new CoapMessageCode(CoapMessageCodeClass.ClientError, 6);
        public static readonly CoapMessageCode RequestEntityIncomplete = new CoapMessageCode(CoapMessageCodeClass.ClientError, 8);
        public static readonly CoapMessageCode PreconditionFailed = new CoapMessageCode(CoapMessageCodeClass.ClientError, 12);
        public static readonly CoapMessageCode RequestEntityTooLarge = new CoapMessageCode(CoapMessageCodeClass.ClientError, 13);
        public static readonly CoapMessageCode UnsupportedContentFormat = new CoapMessageCode(CoapMessageCodeClass.ClientError, 15);

        // 5.xx Server Error
        public static readonly CoapMessageCode InternalServerError = new CoapMessageCode(CoapMessageCodeClass.ServerError, 0);
        public static readonly CoapMessageCode NotImplemented = new CoapMessageCode(CoapMessageCodeClass.ServerError, 1);
        public static readonly CoapMessageCode BadGateway = new CoapMessageCode(CoapMessageCodeClass.ServerError, 2);
        public static readonly CoapMessageCode ServiceUnavailable = new CoapMessageCode(CoapMessageCodeClass.ServerError, 3);
        public static readonly CoapMessageCode GatewayTimeout = new CoapMessageCode(CoapMessageCodeClass.ServerError, 4);
        public static readonly CoapMessageCode ProxyingNotSupported = new CoapMessageCode(CoapMessageCodeClass.ServerError, 5);
    }
}
