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
using CoAPNet.Options;

namespace CoAPNet
{
    /// <summary>
    /// Metadata about a CoAP resource to be used by <see cref="CoreLinkFormat"/> when generating CoRE Link-Format data at <c>/.well-known/core</c>
    /// </summary>
    public class CoapResourceMetadata
    {
        /// <summary>
        /// Relative (to this host) or absolute (external host) 
        /// </summary>
        public Uri UriReference { get; }

        /// <summary>
        /// Link relation type identifies the semantics of a link. They can also be used to indicate that the target resource has particular attributes, or exhibits particular behaviours.
        /// </summary>
        public virtual IList<string> Rel { get; set; } = new List<string>();

        /// <summary>
        /// When present, is used to associate a link's context with another resource
        /// </summary>
        public virtual string Anchor { get; set; }

        [Obsolete("Is deprecated by this specification because it often confuses authors and readers; in most cases, using a separate relation type is preferable.")]
        public virtual IList<string> Rev { get; } = new List<string>();

        /// <summary>
        /// When present, is a hint indicating what the language of the result of dereferencing the link should be. Note that this is only a hint
        /// </summary>
        public virtual string HrefLang { get; set; }

        /// <summary>
        /// When present, is used to indicate intended destination medium or media for style information.
        /// </summary>
        public virtual string Media { get; set; }

        /// <summary>
        /// When present, is used to label the destination of a link such that it can be used as a human-readable identifier (e.g., a menu entry).
        /// </summary>
        public virtual string Title { get; set; }

        /// <summary>
        /// The <c>title*</c> parameter can be used to encode this label in a different character set, and/or contain language information
        /// </summary>
        public virtual string TitleExt { get; set; }

        /// <summary>
        /// When present, is a hint indicating what the media type of the result of dereferencing the link should be. Note that this is only a hint
        /// </summary>
        public virtual string Type { get; set; }

        /// <summary>
        ///  The Resource Type <c>rt</c> attribute is an opaque string used to assign an application-specific semantic type to a resource.
        /// </summary>
        public virtual IList<string> ResourceTypes { get; set; } = new List<string>();

        /// <summary>
        /// The Interface Description <c>if</c> attribute is an opaque string used to provide a name or URI indicating a specific interface definition used to interact with the target resource.
        /// </summary>
        public virtual IList<string> InterfaceDescription { get; set; } = new List<string>();

        /// <summary>
        /// The maximum size estimate attribute <c>sz</c> gives an indication of the maximum size of the resource representation returned by performing a GET on the target URI.
        /// </summary>
        public virtual ulong MaxSize { get; set; }

        /// <summary>
        /// The Content-Format code "ct" attribute provides a hint about the Content-Formats this resource returns. Note that this is only a hint, and it does not override the Content-Format Option of a CoAP response obtained by actually requesting the representation of the resource.
        /// </summary>
        public virtual IList<ContentFormatType> SuggestedContentTypes { get; set; } = new List<ContentFormatType>();

        public virtual Dictionary<string, string> Extentions { get; set; } = new Dictionary<string, string>();

        public CoapResourceMetadata(string uri)
            : this(new Uri(uri, UriKind.RelativeOrAbsolute)) { }

        public CoapResourceMetadata(Uri uri)
        {
            UriReference = uri;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return UriReference.GetHashCode();
        }
    }
}