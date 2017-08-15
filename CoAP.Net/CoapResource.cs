using System.Collections.Generic;
using System.Linq;
using CoAPNet.Options;

namespace CoAPNet
{
    public class CoapResource
    {
        public string URIReference { get; }

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

        public CoapResource(string uri)
        {
            URIReference = uri;
        }

        private bool nullableSequenceEquals<T>(ICollection<T> a, ICollection<T> b) {
            return (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b));
        }

        public override bool Equals(object obj)
        {
            var other = obj as CoapResource;

            if (other == null)
                return base.Equals(obj);

            if (URIReference != other.URIReference)
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
            if (!nullableSequenceEquals(Rel, other.Rel))
                return false;
            if (!nullableSequenceEquals(Rev, other.Rev))
                return false;
            if (!nullableSequenceEquals(ResourceTypes, other.ResourceTypes))
                return false;
            if (!nullableSequenceEquals(InterfaceDescription, other.InterfaceDescription))
                return false;
            if (!nullableSequenceEquals(SuggestedContentTypes, other.SuggestedContentTypes))
                return false;
            if (MaxSize != other.MaxSize)
                return false;
            if (!Enumerable.SequenceEqual<KeyValuePair<string, string>>(Extentions, other.Extentions))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return URIReference.GetHashCode();
        }
    }
}