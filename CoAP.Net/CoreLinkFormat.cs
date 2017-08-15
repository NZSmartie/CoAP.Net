using System;
using System.Collections.Generic;
using System.Linq;

namespace CoAPNet
{
    public static class CoreLinkFormat
    {
        private enum FormatState { LinkValue, LinkParam }

        public static List<CoapResource> Parse(string message)
        {
            var state = FormatState.LinkValue;
            var mPos = 0;

            var result = new List<CoapResource>();
            CoapResource currentResource = null;
            try
            {
                var run = true;
                do
                {
                    if (mPos >= message.Length)
                        break;

                    int mSeek;
                    switch (state)
                    {
                        case FormatState.LinkValue:
                            if (message[mPos++] != '<')
                                throw new ArgumentException($"Expected link-value '<' at pos {mPos}");
                            mSeek = message.IndexOf('>', mPos);
                            if (currentResource != null)
                                result.Add(currentResource);
                            currentResource = new CoapResource(message.Substring(mPos, mSeek - mPos));
                            mPos = mSeek + 1;
                            break;
                        case FormatState.LinkParam:
                            if (currentResource == null)
                                throw new InvalidOperationException();

                            mSeek = message.IndexOf('=', mPos);
                            var param = message.Substring(mPos, mSeek - mPos);

                            mPos = mSeek + 1;
                            mSeek = message.IndexOfAny(new char[] { ',', ';' }, mPos);
                            if (mSeek == -1)
                                mSeek = message.Length;
                            var value = message.Substring(mPos, mSeek - mPos);

                            switch (param)
                            {
                                case "if":
                                    value = value.Substring(1, value.Length - 2);
                                    foreach (var s in value.Split(' '))
                                        currentResource.InterfaceDescription.Add(s);
                                    break;
                                case "rt":
                                    value = value.Substring(1, value.Length - 2);
                                    foreach (var s in value.Split(' '))
                                        currentResource.ResourceTypes.Add(s);
                                    break;
                                case "rev":
                                    if (currentResource.Rev.Count == 0)
                                    {
                                        value = value.Substring(1, value.Length - 2);
                                        foreach (var s in value.Split(' '))
                                            currentResource.Rev.Add(s);
                                    }
                                    break;
                                case "rel":
                                    if (currentResource.Rel.Count == 0)
                                    {
                                        value = value.Substring(1, value.Length - 2);
                                        foreach (var s in value.Split(' '))
                                            currentResource.Rel.Add(s);
                                    }
                                    break;
                                case "anchor":
                                    currentResource.Anchor = value.Substring(1, value.Length - 2);
                                    break;
                                case "hreflang":
                                    // Much easier to let libraries offload language formatting stuff
                                    currentResource.HrefLang = new System.Globalization.CultureInfo(value).Name.ToLower();
                                    break;
                                case "media":
                                    if(value[0] == '"')
                                    {
                                        if (value[value.Length - 1] != '"')
                                            throw new ArgumentException($"Expected MediaDesc DQUOTE '\"' at pos {mSeek}");
                                        value = value.Substring(1, value.Length - 2);
                                    }
                                    currentResource.Media = value;
                                    break;
                                case "title":
                                    if (value[0] != '"' )
                                        throw new ArgumentException($"Expected QuotedString DQUOTE '\"' at pos {mPos}");
                                    if (value[value.Length - 1] != '"')
                                        throw new ArgumentException($"Expected QuotedString DQUOTE '\"' at pos {mSeek}");
                                    currentResource.Title = value.Substring(1, value.Length - 2);
                                    break;
                                case "title*":
                                    // Todo: No idea what to do here...?
                                    var charset = value.Substring(0, value.IndexOf('\''));
                                    var lang = value.Substring(charset.Length + 1, value.IndexOf('\'', charset.Length + 1) - charset.Length - 1);
                                    value = value.Substring(charset.Length + lang.Length + 3, value.Length - charset.Length - lang.Length - 4);

                                    //System.Diagnostics.Debug.WriteLine("title* = {3}\n\tCharset: {0}\n\tLanguage: {1}\n\tValue: {2}", 
                                    //    charset, lang, value, Uri.UnescapeDataString(value));

                                    currentResource.TitleExt = Uri.UnescapeDataString(value);
                                    break;
                                case "type":
                                    if (value[0] == '"')
                                    {
                                        if (value[value.Length - 1] != '"')
                                            throw new ArgumentException($"Expected Type DQUOTE '\"' at pos {mSeek}");
                                        value = value.Substring(1, value.Length - 2);
                                    }
                                    currentResource.Type = value;
                                    break;
                                case "sz":
                                    if (value[0] == '0' && value.Length != 1)
                                        throw new ArgumentException($"cardinal may not start with '0' unless at pos {mSeek}");
                                    if(!ulong.TryParse(value, out var maxSize))
                                        throw new ArgumentException($"Could not parse cardinal at pos {mSeek}");
                                    currentResource.MaxSize = maxSize;
                                    break;
                                case "ct":
                                    int ct = 0;
                                    currentResource.SuggestedContentTypes.Clear();

                                    if (value[0] == '"')
                                    {
                                        if (value[value.Length - 1] != '"')
                                            throw new ArgumentException($"Expected Type DQUOTE '\"' at pos {mSeek}");

                                        foreach (var contentFormatType in value
                                            .Substring(1, value.Length - 2)
                                            .Split(' ')
                                            .Select(s => (Options.ContentFormatType) int.Parse(s)))
                                        {
                                            currentResource.SuggestedContentTypes.Add(contentFormatType);
                                        }
                                    }
                                    else
                                    {
                                        if (value[0] == '0' && value.Length != 1)
                                            throw new ArgumentException($"cardinal may not start with '0' unless at pos {mSeek}");
                                        if (!int.TryParse(value, out ct))
                                            throw new ArgumentException($"Could not parse cardinal at pos {mSeek}");

                                        currentResource.SuggestedContentTypes.Add(ct);
                                    }
                                    break;
                                default:
                                    if (value.Length == 1)
                                    {
                                        if (!char.IsLetterOrDigit(value[0]) && !new [] { '!', '#', '$', '%', '&', '\'', '(', ')', '*', '+', '-', '.', '/', ':', '<', '=', '>', '?', '@', '[', ']', '^', '_', '`', '{', '|', '}', '~' }.Contains(value[0]))
                                            throw new ArgumentException($"PToken contains invalid character '{value[0]}' at pos {mSeek}");
                                        currentResource.Extentions.Add(param, value);
                                    }
                                    else
                                    {
                                        if (value[0] != '"')
                                            throw new ArgumentException($"Expected QuotedString DQUOTE '\"' at pos {mPos}");
                                        if (value[value.Length - 1] != '"')
                                            throw new ArgumentException($"Expected QuotedString DQUOTE '\"' at pos {mSeek}");
                                        currentResource.Extentions.Add(param, value.Substring(1, value.Length - 2));
                                    }
                                    break;
                            }

                            mPos = mSeek;
                            break;
                        default:
                            run = false;
                            break;
                    }
                    if (message[mPos] == ';')
                        state = FormatState.LinkParam;
                    else if (message[mPos] == ',')
                        state = FormatState.LinkValue;
                    else
                        throw new ArgumentException($"Invalid character '{message[mPos]}' at pos {mPos}");
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
