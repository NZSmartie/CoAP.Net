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
using System.Linq;

namespace CoAPNet
{
    /// <summary>
    /// Represents CoAP specific errors that occur during application execution.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CoapException : Exception
    {
        /// <summary>
        /// The response <see cref="CoapMessageCode"/> that was resturned from the remote endpoint.
        /// </summary>
        public CoapMessageCode ResponseCode { get; }

        /// <summary>
        /// Initialise a blank exception. (this is strongly discourages as it lacks detail about the error)
        /// </summary>
        public CoapException()
        {
            ResponseCode = CoapMessageCode.InternalServerError;
        }

        /// <summary>
        /// Initialise an exception with a message describing the error
        /// </summary>
        /// <param name="message"></param>
        public CoapException(string message) : base(message)
        {
            ResponseCode = CoapMessageCode.InternalServerError;
        }

        /// <summary>
        /// Initialise an exception with a message describing the error and the <see cref="CoapMessageCode"/> returned from the remote endpoint.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="responseCode"></param>
        public CoapException(string message, CoapMessageCode responseCode) : base(message)
        {
            ResponseCode = responseCode;
        }

        /// <summary>
        /// Initialise an exception with a message describing the error with an internally caught excpetion.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public CoapException(string message, Exception innerException) : base(message, innerException)
        {
            ResponseCode = CoapMessageCode.InternalServerError;
        }

        /// <summary>
        /// Initialise an exception with a message describing the error with an internally caught excpetion and the <see cref="CoapMessageCode"/> returned from the remote endpoint.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <param name="responseCode"></param>
        public CoapException(string message, Exception innerException, CoapMessageCode responseCode) : base(message, innerException)
        {
            ResponseCode = responseCode;
        }

        /// <summary>
        /// Creates an <see cref="CoapException"/> with details populated from <paramref name="message"/> with an optional inner-exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerExcpetion"></param>
        /// <returns></returns>
        public static CoapException FromCoapMessage(CoapMessage message, Exception innerExcpetion = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var errorMessage = $"({message.Code.Class}.{message.Code.Detail:D2})";
            var contentFormat = message.Options.Get<Options.ContentFormat>();

            if (contentFormat != null && message.Payload != null)
            {
                if (contentFormat.MediaType == Options.ContentFormatType.TextPlain)
                    errorMessage += System.Text.Encoding.UTF8.GetString(message.Payload);
                else
                    errorMessage += string.Join(", ", message.Payload.Select(b => $"0x{b:X2}"));
            }

            return new CoapException(errorMessage, innerExcpetion, message.Code);
        }
    }
}