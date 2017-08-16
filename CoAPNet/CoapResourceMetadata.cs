using System;
using System.Collections.Generic;
using System.Linq;
using CoAPNet.Options;

namespace CoAPNet
{
    public class CoapResourceMetadata
    {
        public Uri UriReference { get; }

        public IList<string> Rel { get; } = new List<string>();

        public string Anchor { get; set; }

        public IList<string> Rev { get; } = new List<string>();

        public string HrefLang { get; set; }

        public string Media { get; set; }

        public string Title { get; set; }

        public string TitleExt { get; set; }

        public string Type { get; set; }

        public IList<string> ResourceTypes { get; } = new List<string>();

        public IList<string> InterfaceDescription { get; } = new List<string>();

        public ulong MaxSize { get; set; }

        public IList<ContentFormatType> SuggestedContentTypes { get; } = new List<ContentFormatType>();

        public Dictionary<string, string> Extentions = new Dictionary<string, string>();

        public CoapResourceMetadata(string uri)
            : this(new Uri(uri, UriKind.RelativeOrAbsolute)) { }

        public CoapResourceMetadata(Uri uri)
        {
            UriReference = uri;
        }

        public override bool Equals(object obj)
        {
            var other = obj as CoapResourceMetadata;

            if (other == null)
                return false;
            if (UriReference != other.UriReference)
                return false;
            if (Anchor != other.Anchor)
                return false;
            if (HrefLang != other.HrefLang)
                return false;
            if (Media != other.Media)
                return false;
            if (Title != other.Title)
                return false;
            if (TitleExt != other.TitleExt)
                return false;
            if (Type != other.Type)
                return false;
            if (!Rel.SequenceEqual(other.Rel))
                return false;
            if (!Rev.SequenceEqual(other.Rev))
                return false;
            if (!ResourceTypes.SequenceEqual(other.ResourceTypes))
                return false;
            if (!InterfaceDescription.SequenceEqual(other.InterfaceDescription))
                return false;
            if (!SuggestedContentTypes.SequenceEqual(other.SuggestedContentTypes))
                return false;
            if (MaxSize != other.MaxSize)
                return false;
            if (!Extentions.SequenceEqual(other.Extentions))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return UriReference.GetHashCode();
        }
    }
}