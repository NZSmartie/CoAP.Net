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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CoAPNet.Utils;

namespace CoAPNet
{
    public class CoapHandler : ICoapHandler
    {
        public Uri BaseUri { get; }

        private int _messageId;

        public CoapHandler()
            : this(new Uri("coap://localhost/"))
        { }

        public CoapHandler(Uri baseUri)
        {
            BaseUri = baseUri;
            _messageId = new Random().Next() & 0xFFFF;
        }

        protected virtual CoapMessage HandleRequest(CoapMessage message)
        {
            return CoapMessageUtility.CreateMessage(CoapMessageCode.NotFound, $"Resouce {message.GetUri()} was not found");
        }

        private int GetNextMessageId()
        {
            return Interlocked.Increment(ref _messageId) & ushort.MaxValue;
        }

        public async Task ProcessRequestAsync(ICoapConnectionInformation connection, byte[] payload)
        {
            CoapMessage result = null;
            var message = new CoapMessage();
            try
            {
                message.Deserialise(payload);

                //TODO: check if message is multicast, ignore Confirmable requests and delay response

                if (!message.Code.IsRequest())
                {
                    // TODO: send CoapMessageCode.Reset or ignore them, i dunno
                    throw new NotImplementedException("TODO: Send CoapMessageCode.Reset or ignore them");
                }

                result = HandleRequest(message);

                
            }
            catch (Exception ex)
            {
                if (ex is CoapException || ex is NotImplementedException)
                {
                    result = CoapMessageUtility.FromException(ex);
                }
                else
                {
                    result = CoapMessageUtility.CreateMessage(CoapMessageCode.InternalServerError,
                        "An unexpected error occured", CoapMessageType.Reset);
                    throw;
                }
            }
            finally
            {
                Debug.Assert(result != null);

                if (message.Type == CoapMessageType.Confirmable)
                {
                    if (result.Type != CoapMessageType.Reset)
                        result.Type = CoapMessageType.Acknowledgement;

                    // TODO: create unit tests to ensure message.Id and message.Token are set when exceptions are thrown
                    result.Id = message.Id;
                }
                else
                {
                    result.Id = GetNextMessageId();
                }

                result.Token = message.Token;

                await connection.LocalEndpoint.SendAsync(
                    new CoapPacket
                    {
                        Endpoint = connection.RemoteEndpoint,
                        Payload = result.Serialise()
                    });
            }
        }
    }
}