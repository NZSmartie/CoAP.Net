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