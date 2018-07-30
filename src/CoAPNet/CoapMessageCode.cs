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
    public readonly struct CoapMessageCode
    {
        /// <summary>
        /// The code class that disguinshes between a request, success, client error or server error.
        /// </summary>
        public readonly int Class;

        /// <summary>
        /// Detail of the message code. See <see cref="CoapMessageCode"/> public methods for existing codes.
        /// </summary>
        public readonly int Detail;

        /// <summary>
        /// Initalise a new CoapMessage Code with a code-class and detail value to encode in a <see cref="CoapMessage"/>
        /// </summary>
        /// <param name="codeClass">A supported code-class. See <see cref="CoapMessageCodeClass"/></param>
        /// <param name="detail"></param>
        public CoapMessageCode(in int codeClass, in int detail)
        {
            Class = codeClass;
            Detail = detail;
        }

        /// <summary>
        /// Initalise a new CoapMessage Code with a code-class and detail value to encode in a <see cref="CoapMessage"/>
        /// </summary>
        /// <param name="codeClass">A supported code-class.</param>
        /// <param name="detail"></param>
        public CoapMessageCode(in CoapMessageCodeClass codeClass, in int detail)
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
            if (obj is CoapMessageCode code)
                return Class == code.Class &&
                       Detail == code.Detail;
            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = 867851563;
            hashCode = hashCode * -1521134295 + Class.GetHashCode();
            hashCode = hashCode * -1521134295 + Detail.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Compare <paramref name="a"/> for equality with <paramref name="b"/>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(in CoapMessageCode a, in CoapMessageCode b)
        {
            return a.Class == b.Class && a.Detail == b.Detail;
        }

        /// <summary>
        /// Compare <paramref name="a"/> for inequality with <paramref name="b"/>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(CoapMessageCode a, CoapMessageCode b)
        {
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
        /// <summary>
        /// Like HTTP 201 "Created", but only used in response to <see cref="Post" /> and <see cref="Put"/> requests.The <see cref="CoapMessage.Payload"/> returned with the response, if any, is a representation of the action result.
        /// If the response includes one or more <see cref="Options.LocationPath"/> and/or <see cref="Options.LocationQuery"/> <see cref="CoapMessage.Options"/>, the values of these options specify the location at which the resource was created. 
        /// Otherwise, the resource was created at the request URI. 
        /// A cache receiving this response MUST mark any stored response for the created resource as not fresh.
        /// </summary>
        /// <remarks>This response is not cacheable.</remarks>
        public static readonly CoapMessageCode Created = new CoapMessageCode(CoapMessageCodeClass.Success, 1);

        /// <summary>
        /// This Response Code is like HTTP 204 "No Content" but only used in response to requests that cause the resource to cease being available, such as <see cref="Delete"/> and, in certain circumstances, <see cref="Post"/>.
        /// The <see cref="CoapMessage.Payload"/> returned with the response, if any, is a representation of the action result. This response is not cacheable. However, a cache MUST mark any stored response for the deleted resource as not fresh.
        /// </summary>
        public static readonly CoapMessageCode Deleted = new CoapMessageCode(CoapMessageCodeClass.Success, 2);

        /// <summary>
        /// This Response Code is related to HTTP 304 "Not Modified" but only used to indicate that the response identified by the entity-tag identified by the included ETag Option is valid. 
        /// Accordingly, the response MUST include an <see cref="Options.ETag"/> Option and MUST NOT include a <see cref="CoapMessage.Payload"/>. 
        /// When a cache that recognizes and processes the <see cref="Options.ETag"/> response option receives a 2.03 (<see cref="Valid"/>) response, it MUST update the stored response with the value of the <see cref="Options.MaxAge"/> Option included in the response (explicitly, or implicitly as a default value; see also Section 5.6.2). 
        /// For each type of Safe-to-Forward option present in the response, the (possibly empty) set of options of this type that are present in the stored response MUST be replaced with the set of options of this type in the response received.
        /// (Unsafe options may trigger similar option-specific processing as defined by the option.)
        /// </summary>
        public static readonly CoapMessageCode Valid = new CoapMessageCode(CoapMessageCodeClass.Success, 3);

        /// <summary>
        /// This Response Code is like HTTP 204 "No Content" but only used in response to <see cref="Post"/> and <see cref="Put"/> requests. 
        /// The <see cref="CoapMessage.Payload"/> returned with the response, if any, is a representation of the action result. 
        /// This response is not cacheable. However, a cache MUST mark any stored response for the changed resource as not fresh.
        /// </summary>
        public static readonly CoapMessageCode Changed = new CoapMessageCode(CoapMessageCodeClass.Success, 4);

        /// <summary>
        /// This Response Code is like HTTP 200 "OK" but only used in response to <see cref="Get"/> requests. 
        /// The payload returned with the response is a representation of the target resource. 
        /// This response is cacheable: Caches can use the <see cref="Options.MaxAge"/> Option to determine freshness (see Section 5.6.1) and (if present) the <see cref="Options.ETag"/> Option for validation (see Section 5.6.2).
        /// </summary>
        public static readonly CoapMessageCode Content = new CoapMessageCode(CoapMessageCodeClass.Success, 5);

        /// <summary>
        /// This success status code indicates that the transfer of this block of the request body was successful and that the server encourages sending further blocks, but that a final outcome of the whole block-wise request cannot yet be determined. 
        /// No payload is returned with this response code. 
        /// </summary>
        public static readonly CoapMessageCode Continue = new CoapMessageCode(CoapMessageCodeClass.Success, 31);

        // 4.xx Client Error
        /// <summary>
        /// This Response Code is Like HTTP 400 "Bad Request".
        /// </summary>
        public static readonly CoapMessageCode BadRequest = new CoapMessageCode(CoapMessageCodeClass.ClientError, 0);

        /// <summary>
        /// The client is not authorized to perform the requested action. 
        /// The client SHOULD NOT repeat the request without first improving its authentication status to the server. 
        /// Which specific mechanism can be used for this is outside this document's scope; see also Section 9.
        /// </summary>
        public static readonly CoapMessageCode Unauthorized = new CoapMessageCode(CoapMessageCodeClass.ClientError, 1);

        /// <summary>
        /// The request could not be understood by the server due to one or more unrecognized or malformed options. 
        /// The client SHOULD NOT repeat the request without modification. 
        /// </summary>
        public static readonly CoapMessageCode BadOption = new CoapMessageCode(CoapMessageCodeClass.ClientError, 2);

        /// <summary>
        /// This Response Code is like HTTP 403 "Forbidden".
        /// </summary>
        public static readonly CoapMessageCode Forbidden = new CoapMessageCode(CoapMessageCodeClass.ClientError, 3);

        /// <summary>
        /// This Response Code is like HTTP 404 "Not Found". 
        /// </summary>
        public static readonly CoapMessageCode NotFound = new CoapMessageCode(CoapMessageCodeClass.ClientError, 4);

        /// <summary>
        /// This Response Code is like HTTP 405 "Method Not Allowed" but with no parallel to the "Allow" header field.
        /// </summary>
        public static readonly CoapMessageCode MethodNotAllowed = new CoapMessageCode(CoapMessageCodeClass.ClientError, 5);

        /// <summary>
        /// This Response Code is like HTTP 406 "Not Acceptable", but with no response entity.
        /// </summary>
        public static readonly CoapMessageCode NotAcceptable = new CoapMessageCode(CoapMessageCodeClass.ClientError, 6);

        /// <summary>
        /// This new client error status code indicates that the server has not received the blocks of the request body that it needs to proceed. 
        /// The client has not sent all blocks, not sent them in the order required by the server, or has sent them long enough ago that the server has already discarded them. 
        /// </summary>
        public static readonly CoapMessageCode RequestEntityIncomplete = new CoapMessageCode(CoapMessageCodeClass.ClientError, 8);

        /// <summary>
        /// This Response Code is like HTTP 412 "Precondition Failed".
        /// </summary>
        public static readonly CoapMessageCode PreconditionFailed = new CoapMessageCode(CoapMessageCodeClass.ClientError, 12);

        /// <summary>
        /// In Section 5.9.2.9 of [RFC7252], the response code 4.13 (Request Entity Too Large) is defined to be like HTTP 413 "Request Entity Too Large".  
        /// [RFC7252] also recommends that this response SHOULD include a <see cref="Options.Size1"/> Option (Section 4) to indicate the maximum size of request entity the server is able and willing to handle, unless the server is not in a position to make this information available.
        /// The present specification allows the server to return this response code at any time during a Block1 transfer to indicate that it does not currently have the resources to store blocks for a transfer that it would intend to implement in an atomic fashion.
        /// It also allows the server to return a 4.13 response to a request that does not employ Block1 as a hint for the client to try sending Block1.
        /// Finally, a 4.13 response to a request with a <see cref="Options.Block1"/> Option (control usage, see Section 2.3) where the response carries a smaller <see cref="Options.BlockBase.BlockSize"/> in its <see cref="Options.Block1"/> Option is a hint to try that smaller <see cref="Options.BlockBase.BlockSize"/>.
        /// </summary>
        public static readonly CoapMessageCode RequestEntityTooLarge = new CoapMessageCode(CoapMessageCodeClass.ClientError, 13);

        /// <summary>
        /// This Response Code is like HTTP 415 "Unsupported Media Type".
        /// </summary>
        public static readonly CoapMessageCode UnsupportedContentFormat = new CoapMessageCode(CoapMessageCodeClass.ClientError, 15);

        // 5.xx Server Error

        /// <summary>
        /// This Response Code is like HTTP 500 "Internal Server Error".
        /// </summary>
        public static readonly CoapMessageCode InternalServerError = new CoapMessageCode(CoapMessageCodeClass.ServerError, 0);

        /// <summary>
        /// This Response Code is like HTTP 501 "Not Implemented". 
        /// </summary>
        public static readonly CoapMessageCode NotImplemented = new CoapMessageCode(CoapMessageCodeClass.ServerError, 1);

        /// <summary>
        /// This Response Code is like HTTP 502 "Bad Gateway".
        /// </summary>
        public static readonly CoapMessageCode BadGateway = new CoapMessageCode(CoapMessageCodeClass.ServerError, 2);

        /// <summary>
        /// This Response Code is like HTTP 503 "Service Unavailable" but uses the <see cref="Options.MaxAge"/> Option in place of the "Retry-After" header field to indicate the number of seconds after which to retry. 
        /// </summary>
        public static readonly CoapMessageCode ServiceUnavailable = new CoapMessageCode(CoapMessageCodeClass.ServerError, 3);

        /// <summary>
        /// This Response Code is like HTTP 504 "Gateway Timeout".
        /// </summary>
        public static readonly CoapMessageCode GatewayTimeout = new CoapMessageCode(CoapMessageCodeClass.ServerError, 4);

        /// <summary>
        /// The server is unable or unwilling to act as a forward-proxy for the URI specified in the <see cref="Options.ProxyUri"/> Option or using <see cref="Options.ProxyScheme"/> (see Section 5.10.2).
        /// </summary>
        public static readonly CoapMessageCode ProxyingNotSupported = new CoapMessageCode(CoapMessageCodeClass.ServerError, 5);
    }
}
