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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoAPNet.Options;

namespace CoAPNet.Server
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
                Options = { new ContentFormat(ContentFormatType.ApplicationLinkFormat) },
                Payload = Encoding.UTF8.GetBytes(
                    CoreLinkFormat.ToCoreLinkFormat(_resourceHandler.Resources.Select(r => r.Metadata)))
            };
        }
    }

    public class CoapResourceHandler : CoapHandler
    {
        public StringComparison UriPathComparison { get; set; } = StringComparison.Ordinal;

        public readonly IList<CoapResource> Resources = new List<CoapResource>();

        public CoapResourceHandler()
            : this(new Uri("coap://localhost/"))
        { }

        public CoapResourceHandler(Uri baseUri)
            :base(baseUri)
        {
            Resources.Add(new CoapResourceCoreListing(baseUri, this));
        }

        protected override async Task<CoapMessage> HandleRequestAsync(ICoapConnectionInformation connectionInformation, CoapMessage message)
        {
            var resource = Resources.FirstOrDefault(r =>
                Uri.Compare(
                    new Uri(BaseUri, r.Metadata.UriReference),
                    new Uri(BaseUri, message.GetUri()),
                    UriComponents.Path,
                    UriFormat.SafeUnescaped,
                    UriPathComparison) == 0);

            if (resource == null)
                return CoapMessage.Create(CoapMessageCode.NotFound,
                    $"Resouce {message.GetUri()} was not found");

            // ReSharper disable once SwitchStatementMissingSomeCases
            if (message.Code == CoapMessageCode.Get)
                return await resource.GetAsync(message, connectionInformation);
            if (message.Code == CoapMessageCode.Post)
                return await resource.PostAsync(message, connectionInformation);
            if (message.Code == CoapMessageCode.Put)
                return await resource.PutAsync(message, connectionInformation);
            if (message.Code == CoapMessageCode.Delete)
                return await resource.DeleteAsync(message, connectionInformation);

            throw new InvalidOperationException();
        }
    }
}