using System;

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

        public virtual CoapMessage Get()
        {
            throw new NotImplementedException();
        }

        public virtual CoapMessage Put()
        {
            throw new NotImplementedException();
        }

        public virtual CoapMessage Post()
        {
            throw new NotImplementedException();
        }

        public virtual CoapMessage Delete()
        {
            throw new NotImplementedException();
        }
    }
}