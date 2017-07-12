using System;
using System.Collections.Generic;
using System.Linq;

namespace CoAPNet
{
    public class CoapResource
    {
        public string URIReference { get; }

        public List<string> Rel;

        public string Anchor;

        public List<string> Rev;

        public string HrefLang;

        public string Media;

        public string Title;

        public string TitleExt;

        public string Type;

        public List<string> ResourceTypes;

        public List<string> InterfaceDescription;

        public ulong MaxSize;

        public List<Options.ContentFormatType> SuggestedContentTypes;

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
            if (!Extentions.SequenceEqual(other.Extentions))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return URIReference.GetHashCode();
        }
    }

    public static class CoreLinkFormat
    {
        private enum _formatState { LinkValue, LinkParam }

        public static List<CoapResource> Parse(string message)
        {
            var state = _formatState.LinkValue;
            int mPos = 0, mSeek;
            string param, value;

            var result = new List<CoapResource>();
            CoapResource currentResource = null;
            try
            {
                var run = true;
                do
                {
                    if (mPos >= message.Length)
                        break;

                    switch (state)
                    {
                        case _formatState.LinkValue:
                            if (message[mPos++] != '<')
                                throw new ArgumentException(string.Format("Expected link-value '<' at pos {0}", mPos));

                            mSeek = message.IndexOf('>', mPos);
                            if (currentResource != null)
                                result.Add(currentResource);
                            currentResource = new CoapResource(message.Substring(mPos, mSeek - mPos));
                            mPos = mSeek + 1;
                            break;
                        case _formatState.LinkParam:
                            mSeek = message.IndexOf('=', mPos);
                            param = message.Substring(mPos, mSeek - mPos);

                            mPos = mSeek + 1;
                            mSeek = message.IndexOfAny(new char[] { ',', ';' }, mPos);
                            if (mSeek == -1)
                                mSeek = message.Length;
                            value = message.Substring(mPos, mSeek - mPos);

                            if(param == "if")
                            {
                                value = value.Substring(1, value.Length - 2);
                                currentResource.InterfaceDescription = value.Split(new char[] { ' ' }).ToList();
                            }else if (param == "rt")
                            {
                                value = value.Substring(1, value.Length - 2);
                                currentResource.ResourceTypes = value.Split(new char[] { ' ' }).ToList();
                            } else if (param == "rev")
                            {
                                if (currentResource.Rev == null)
                                {
                                    value = value.Substring(1, value.Length - 2);
                                    currentResource.Rev = value.Split(new char[] { ' ' }).ToList();
                                }
                            }
                            else if (param == "rel")
                            {
                                if (currentResource.Rel == null)
                                {
                                    value = value.Substring(1, value.Length - 2);
                                    currentResource.Rel = value.Split(new char[] { ' ' }).ToList();
                                }
                            }
                            else if (param == "anchor")
                            {
                                currentResource.Anchor = value.Substring(1, value.Length - 2);
                            }
                            else if (param == "hreflang")
                            {
                                // Much easier to let libraries offload language formatting stuff
                                currentResource.HrefLang = new System.Globalization.CultureInfo(value).Name.ToLower();
                            }
                            else if (param == "media")
                            {
                                if(value[0] == '"')
                                {
                                    if (value[value.Length - 1] != '"')
                                        throw new ArgumentException(string.Format("Expected MediaDesc DQUOTE '\"' at pos {0}", mSeek));
                                    value = value.Substring(1, value.Length - 2);
                                }
                                currentResource.Media = value;
                            }
                            else if (param == "title")
                            {
                                if (value[0] != '"' )
                                    throw new ArgumentException(string.Format("Expected QuotedString DQUOTE '\"' at pos {0}", mPos));
                                if (value[value.Length - 1] != '"')
                                    throw new ArgumentException(string.Format("Expected QuotedString DQUOTE '\"' at pos {0}", mSeek));
                                currentResource.Title = value.Substring(1, value.Length - 2);
                            }
                            else if (param == "title*")
                            {
                                // Todo: No idea what to do here...?
                                var charset = value.Substring(0, value.IndexOf('\''));
                                var lang = value.Substring(charset.Length + 1, value.IndexOf('\'', charset.Length + 1) - charset.Length - 1);
                                value = value.Substring(charset.Length + lang.Length + 3, value.Length - charset.Length - lang.Length - 4);

                                //System.Diagnostics.Debug.WriteLine("title* = {3}\n\tCharset: {0}\n\tLanguage: {1}\n\tValue: {2}", 
                                //    charset, lang, value, Uri.UnescapeDataString(value));

                                currentResource.TitleExt = Uri.UnescapeDataString(value);
                            }
                            else if (param == "type")
                            {
                                if (value[0] == '"')
                                {
                                    if (value[value.Length - 1] != '"')
                                        throw new ArgumentException(string.Format("Expected Type DQUOTE '\"' at pos {0}", mSeek));
                                    value = value.Substring(1, value.Length - 2);
                                }
                                currentResource.Type = value;
                            }
                            else if (param == "sz")
                            {
                                if (value[0] == '0' && value.Length != 1)
                                    throw new ArgumentException(string.Format("cardinal may not start with '0' unless at pos {0}", mSeek));
                                if(!ulong.TryParse(value, out currentResource.MaxSize))
                                    throw new ArgumentException(string.Format("Could not parse cardinal at pos {0}", mSeek));
                            }
                            else if(param == "ct")
                            {
                                int ct = 0;
                                currentResource.SuggestedContentTypes = new List<Options.ContentFormatType>();

                                if (value[0] == '"')
                                {
                                    if (value[value.Length - 1] != '"')
                                        throw new ArgumentException(string.Format("Expected Type DQUOTE '\"' at pos {0}", mSeek));
                                    currentResource.SuggestedContentTypes.AddRange(
                                        value
                                            .Substring(1, value.Length - 2)
                                            .Split(new char[] { ' ' })
                                            .Select(s => (Options.ContentFormatType)int.Parse(s))
                                        );
                                }
                                else
                                {
                                    if (value[0] == '0' && value.Length != 1)
                                        throw new ArgumentException(string.Format("cardinal may not start with '0' unless at pos {0}", mSeek));
                                    if (!int.TryParse(value, out ct))
                                        throw new ArgumentException(string.Format("Could not parse cardinal at pos {0}", mSeek));

                                    currentResource.SuggestedContentTypes.Add((Options.ContentFormatType)ct);
                                }
                            }
                            else
                            {
                                if (value.Length == 1)
                                {
                                    if (!char.IsLetterOrDigit(value[0]) && !new char[] { '!', '#', '$', '%', '&', '\'', '(', ')', '*', '+', '-', '.', '/', ':', '<', '=', '>', '?', '@', '[', ']', '^', '_', '`', '{', '|', '}', '~' }.Contains(value[0]))
                                        throw new ArgumentException(string.Format("PToken contains invalid character '{0}' at pos {1}", value[0], mSeek));
                                    currentResource.Extentions.Add(param, value);
                                }
                                else
                                {
                                    if (value[0] != '"')
                                        throw new ArgumentException(string.Format("Expected QuotedString DQUOTE '\"' at pos {0}", mPos));
                                    if (value[value.Length - 1] != '"')
                                        throw new ArgumentException(string.Format("Expected QuotedString DQUOTE '\"' at pos {0}", mSeek));
                                    currentResource.Extentions.Add(param, value.Substring(1, value.Length - 2));
                                }
                            }

                            mPos = mSeek;
                            break;
                        default:
                            run = false;
                            break;
                    }
                    if (message[mPos] == ';')
                        state = _formatState.LinkParam;
                    else if (message[mPos] == ',')
                        state = _formatState.LinkValue;
                    else
                        throw new ArgumentException(string.Format("Invalid character '{0}' at pos {1}", message[mPos], mPos));
                    mPos++;
                }
                while (run);
            }
            catch(IndexOutOfRangeException)
            {
                // Moving on...
            }

            if (currentResource != null)
                result.Add(currentResource);

            return result;
        }
    }
}
