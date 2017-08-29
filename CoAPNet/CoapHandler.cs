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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoAPNet.Options;
using CoAPNet.Utils;

namespace CoAPNet
{
    /// <summary>
    /// Generates a /.well-known/core resource based on <see cref="CoapResourceHandler.Resources"/>
    /// </summary>
    public class CoapResourceCoreListing : CoapResource
    {
        private readonly CoapResourceHandler _resourceHandler;

        public CoapResourceCoreListing(Uri uri, CoapResourceHandler resourceHandler) 
            : base(new UriBuilder(uri) { Path = "/.well-known/core" }.Uri)
        {
            _resourceHandler = resourceHandler;
            Metadata.SuggestedContentTypes.Add(ContentFormatType.ApplicationLinkFormat);
        }

        public override CoapMessage Get(CoapMessage request)
        {
            // TODO: Allow resources to opt out?
            // TODO: filter result based on get paremeters
            return new CoapMessage
            {
                Code = CoapMessageCode.Content,
                Options = {new ContentFormat(ContentFormatType.ApplicationLinkFormat)},
                Payload = Encoding.UTF8.GetBytes(
                    CoreLinkFormat.ToCoreLinkFormat(_resourceHandler.Resources.Select(r => r.Metadata)))
            };
        }
    }

    public class CoapResourceHandler : CoapHandler
    {
        public readonly IList<CoapResource> Resources = new List<CoapResource>();

        public override CoapResource GetResource(Uri requesstUri)
        {
            return Resources.FirstOrDefault(r =>
                Uri.Compare(
                    new Uri(BaseUri, r.Metadata.UriReference),
                    requesstUri,
                    UriComponents.Path,
                    UriFormat.SafeUnescaped,
                    UriPathComparison) == 0);
        }

        public override Uri BaseUri { get; }

        public CoapResourceHandler()
            : this(new Uri("coap://localhost/"))
        { }

        public CoapResourceHandler(Uri baseUri)
        {
            BaseUri = baseUri;
            Resources.Add(new CoapResourceCoreListing(baseUri, this));
        }
    }

    public class CoapHandler : ICoapHandler
    {
        public virtual CoapResource GetResource(Uri requesstUri)
        {
            throw new NotImplementedException();
        }

        public StringComparison UriPathComparison { get; set; } = StringComparison.Ordinal;

        public virtual Uri BaseUri { get; } = new Uri("coap://localhost/");

        private int _messageId;

        public CoapHandler()
        {
            _messageId = new Random().Next() & 0xFFFF;
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

                var resource = GetResource(new Uri(BaseUri, message.GetUri()));

                if (resource == null)
                {
                    result = CoapMessageUtility.CreateMessage(CoapMessageCode.NotFound,
                        $"Resouce {message.GetUri()} was not found");
                }
                else
                {
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (message.Code)
                    {
                        case CoapMessageCode.Get:
                            result = resource.Get(message);
                            break;
                        case CoapMessageCode.Post:
                            result = resource.Post(message);
                            break;
                        case CoapMessageCode.Put:
                            result = resource.Put(message);
                            break;
                        case CoapMessageCode.Delete:
                            result = resource.Delete(message);
                            break;
                    }
                }
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