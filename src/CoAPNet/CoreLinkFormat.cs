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
using CoAPNet.Utils;

namespace CoAPNet
{
    public static class CoreLinkFormat
    {
        private enum FormatState { LinkValue, LinkParam }

        public static List<CoapResourceMetadata> Parse(string message)
        {
            var state = FormatState.LinkValue;
            var mPos = 0;

            var result = new List<CoapResourceMetadata>();
            CoapResourceMetadata currentResourceMetadata = null;
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
                            if (currentResourceMetadata != null)
                                result.Add(currentResourceMetadata);
                            currentResourceMetadata = new CoapResourceMetadata(message.Substring(mPos, mSeek - mPos));
                            mPos = mSeek + 1;
                            break;
                        case FormatState.LinkParam:
                            if (currentResourceMetadata == null)
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
                                        currentResourceMetadata.InterfaceDescription.Add(s);
                                    break;
                                case "rt":
                                    value = value.Substring(1, value.Length - 2);
                                    foreach (var s in value.Split(' '))
                                        currentResourceMetadata.ResourceTypes.Add(s);
                                    break;
                                case "rel":
                                    if (currentResourceMetadata.Rel.Count == 0)
                                    {
                                        value = value.Substring(1, value.Length - 2);
                                        foreach (var s in value.Split(' '))
                                            currentResourceMetadata.Rel.Add(s);
                                    }
                                    break;
                                case "anchor":
                                    currentResourceMetadata.Anchor = value.Substring(1, value.Length - 2);
                                    break;
                                case "hreflang":
                                    // Much easier to let libraries offload language formatting stuff
                                    currentResourceMetadata.HrefLang = new System.Globalization.CultureInfo(value).Name.ToLower();
                                    break;
                                case "media":
                                    if(value[0] == '"')
                                    {
                                        if (value[value.Length - 1] != '"')
                                            throw new ArgumentException($"Expected MediaDesc DQUOTE '\"' at pos {mSeek}");
                                        value = value.Substring(1, value.Length - 2);
                                    }
                                    currentResourceMetadata.Media = value;
                                    break;
                                case "title":
                                    if (value[0] != '"' )
                                        throw new ArgumentException($"Expected QuotedString DQUOTE '\"' at pos {mPos}");
                                    if (value[value.Length - 1] != '"')
                                        throw new ArgumentException($"Expected QuotedString DQUOTE '\"' at pos {mSeek}");
                                    currentResourceMetadata.Title = value.Substring(1, value.Length - 2);
                                    break;
                                case "title*":
                                    // TODO: No idea what to do here...?
                                    var charset = value.Substring(0, value.IndexOf('\''));
                                    var lang = value.Substring(charset.Length + 1, value.IndexOf('\'', charset.Length + 1) - charset.Length - 1);
                                    value = value.Substring(charset.Length + lang.Length + 3, value.Length - charset.Length - lang.Length - 4);

                                    //System.Diagnostics.Debug.WriteLine("title* = {3}\n\tCharset: {0}\n\tLanguage: {1}\n\tValue: {2}", 
                                    //    charset, lang, value, Uri.UnescapeDataString(value));

                                    currentResourceMetadata.TitleExt = Uri.UnescapeDataString(value);
                                    break;
                                case "type":
                                    if (value[0] == '"')
                                    {
                                        if (value[value.Length - 1] != '"')
                                            throw new ArgumentException($"Expected Type DQUOTE '\"' at pos {mSeek}");
                                        value = value.Substring(1, value.Length - 2);
                                    }
                                    currentResourceMetadata.Type = value;
                                    break;
                                case "sz":
                                    if (value[0] == '0' && value.Length != 1)
                                        throw new ArgumentException($"cardinal may not start with '0' unless at pos {mSeek}");
                                    if(!ulong.TryParse(value, out var maxSize))
                                        throw new ArgumentException($"Could not parse cardinal at pos {mSeek}");
                                    currentResourceMetadata.MaxSize = maxSize;
                                    break;
                                case "ct":
                                    int ct = 0;
                                    currentResourceMetadata.SuggestedContentTypes.Clear();

                                    if (value[0] == '"')
                                    {
                                        if (value[value.Length - 1] != '"')
                                            throw new ArgumentException($"Expected Type DQUOTE '\"' at pos {mSeek}");

                                        foreach (var contentFormatType in value
                                            .Substring(1, value.Length - 2)
                                            .Split(' ')
                                            .Select(s => (Options.ContentFormatType) int.Parse(s)))
                                        {
                                            currentResourceMetadata.SuggestedContentTypes.Add(contentFormatType);
                                        }
                                    }
                                    else
                                    {
                                        if (value[0] == '0' && value.Length != 1)
                                            throw new ArgumentException($"cardinal may not start with '0' unless at pos {mSeek}");
                                        if (!int.TryParse(value, out ct))
                                            throw new ArgumentException($"Could not parse cardinal at pos {mSeek}");

                                        currentResourceMetadata.SuggestedContentTypes.Add(ct);
                                    }
                                    break;
                                default:
                                    if (value.Length == 1)
                                    {
                                        if (!char.IsLetterOrDigit(value[0]) && !new [] { '!', '#', '$', '%', '&', '\'', '(', ')', '*', '+', '-', '.', '/', ':', '<', '=', '>', '?', '@', '[', ']', '^', '_', '`', '{', '|', '}', '~' }.Contains(value[0]))
                                            throw new ArgumentException($"PToken contains invalid character '{value[0]}' at pos {mSeek}");
                                        currentResourceMetadata.Extentions.Add(param, value);
                                    }
                                    else
                                    {
                                        if (value[0] != '"')
                                            throw new ArgumentException($"Expected QuotedString DQUOTE '\"' at pos {mPos}");
                                        if (value[value.Length - 1] != '"')
                                            throw new ArgumentException($"Expected QuotedString DQUOTE '\"' at pos {mSeek}");
                                        currentResourceMetadata.Extentions.Add(param, value.Substring(1, value.Length - 2));
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

            if (currentResourceMetadata != null)
                result.Add(currentResourceMetadata);

            return result;
        }

        private static readonly Uri _throwAwayUri = new Uri("coap://localhost/");

        public static string ToCoreLinkFormat(CoapResourceMetadata resource, Uri baseUri = null)
        {
            var message = new StringBuilder();
            try
            {
                if (baseUri == null)
                    baseUri = _throwAwayUri;

                var uri = resource.UriReference.IsAbsoluteUri
                    ? resource.UriReference
                    : new Uri(baseUri, resource.UriReference);

                message.Append(CoapUri.Compare(uri, baseUri, UriComponents.HostAndPort, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0
                    ? $"<{uri.AbsolutePath}>"
                    : $"<{uri}>");

                if(resource.InterfaceDescription.Count > 0)
                message.AppendFormat(";if=\"{0}\"", string.Join(" ", resource.InterfaceDescription));

                if (resource.SuggestedContentTypes.Count == 1)
                    message.Append($";ct={(int)resource.SuggestedContentTypes.First():D}");

                if (resource.SuggestedContentTypes.Count > 1)
                    message.AppendFormat(";ct=\"{0}\"", string.Join(" ", resource.SuggestedContentTypes.Select(ct => ((int) ct).ToString("D"))));

                if (resource.ResourceTypes.Count > 0)
                    message.AppendFormat(";rt=\"{0}\"", string.Join(" ", resource.ResourceTypes));

                if (resource.Rel.Count == 1)
                    message.AppendFormat(";rel={0}", resource.Rel.First());
                if (resource.Rel.Count > 1)
                    message.AppendFormat(";rel=\"{0}\"", string.Join(" ", resource.Rel));

                if (!string.IsNullOrEmpty(resource.Anchor))
                    message.Append($";anchor=\"{resource.Anchor}\"");

                if ((resource.HrefLang ?? string.Empty) != string.Empty)
                    message.Append($";hreflang={resource.HrefLang?.ToLower()}");

                if ((resource.Media ?? string.Empty) != string.Empty)
                    message.Append(resource.Media.Any(c => c == ';' || c == ',')
                        ? $";media=\"{resource.Media}\""
                        : $";media={resource.Media}");

                if ((resource.Title ?? string.Empty) != string.Empty)
                    message.Append($";title=\"{resource.Title}\"");

                //case "title*":
                //    // TODO: No idea what to do here...?
                //    var charset = value.Substring(0, value.IndexOf('\''));
                //    var lang = value.Substring(charset.Length + 1,
                //        value.IndexOf('\'', charset.Length + 1) - charset.Length - 1);
                //    value = value.Substring(charset.Length + lang.Length + 3,
                //        value.Length - charset.Length - lang.Length - 4);

                //    //System.Diagnostics.Debug.WriteLine("title* = {3}\n\tCharset: {0}\n\tLanguage: {1}\n\tValue: {2}", 
                //    //    charset, lang, value, Uri.UnescapeDataString(value));

                //    resource.TitleExt = Uri.UnescapeDataString(value);
                //    break;

                if ((resource.Type ?? string.Empty) != string.Empty)
                    message.Append(resource.Type.Contains(" ")
                        ? $";type=\"{resource.Type}\""
                        : $";type={resource.Type}");

                if (resource.MaxSize != 0)
                    message.Append($";sz={resource.MaxSize:D}");

                
                //default:
                //    if (value.Length == 1)
                //    {
                //        if (!char.IsLetterOrDigit(value[0]) && !new[]
                //        {
                //            '!', '#', '$', '%', '&', '\'', '(', ')', '*', '+', '-', '.', '/', ':',
                //            '<', '=', '>', '?', '@', '[', ']', '^', '_', '`', '{', '|', '}', '~'
                //        }.Contains(value[0]))
                //            throw new ArgumentException(
                //                $"PToken contains invalid character '{value[0]}' at pos {mSeek}");
                //        resource.Extentions.Add(param, value);
                //    }
                //    else
                //    {
                //        if (value[0] != '"')
                //            throw new ArgumentException(
                //                $"Expected QuotedString DQUOTE '\"' at pos {mPos}");
                //        if (value[value.Length - 1] != '"')
                //            throw new ArgumentException(
                //                $"Expected QuotedString DQUOTE '\"' at pos {mSeek}");
                //        resource.Extentions.Add(param,
                //            value.Substring(1, value.Length - 2));
                //    }
                //    break;
            }
            catch (IndexOutOfRangeException)
            {
                // Moving on...
            }
            return message.ToString();
        }

        public static string ToCoreLinkFormat(IEnumerable<CoapResourceMetadata> resources, Uri baseUri = null)
        {
            return string.Join(",", resources.Select(r => ToCoreLinkFormat(r, baseUri)));
        }
    }
}
