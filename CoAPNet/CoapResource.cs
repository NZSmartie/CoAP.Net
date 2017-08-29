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

namespace CoAPNet
{
    public class CoapResource
    {
        public Uri Uri => Metadata.UriReference;

        public CoapResourceMetadata Metadata { get; set; }

        public CoapResource(string uri)
            : this(new Uri(uri, UriKind.Relative)) { }

        public CoapResource(Uri uri)
        {
            Metadata = new CoapResourceMetadata(uri);
        }

        public CoapResource(CoapResourceMetadata metadata)
        {
            Metadata = metadata;
        }

        public virtual CoapMessage Get(CoapMessage request)
        {
            return new CoapMessage
            {
                Code = CoapMessageCode.MethodNotAllowed,
                Token = request.Token
            };
        }

        public virtual CoapMessage Put(CoapMessage request)
        {
            return new CoapMessage
            {
                Code = CoapMessageCode.MethodNotAllowed,
                Token = request.Token
            };
        }

        public virtual CoapMessage Post(CoapMessage request)
        {
            return new CoapMessage
            {
                Code = CoapMessageCode.MethodNotAllowed,
                Token = request.Token
            };
        }

        public virtual CoapMessage Delete(CoapMessage request)
        {
            return new CoapMessage
            {
                Code = CoapMessageCode.MethodNotAllowed,
                Token = request.Token
            };
        }
    }
}