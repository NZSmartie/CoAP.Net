using System;
using System.Linq;

namespace CoAPNet.Utils
{
    /// <summary>
    /// Extensions for easily managing CoAP related resources.
    /// </summary>
    public static class CoapUri
    {
        private static readonly string[] _schemes = {"coap", "coaps"};

        /// <summary>
        /// Supports CoAP scheme <see cref="Uri"/> for comparing to each other.
        /// See <see cref="Uri.Compare(Uri, Uri, UriComponents, UriFormat, StringComparison)"/>
        /// </summary>
        /// <param name="uri1"></param>
        /// <param name="uri2"></param>
        /// <param name="partsToCompare"></param>
        /// <param name="compareFormat"></param>
        /// <param name="comparisonType"></param>
        /// <returns></returns>
        public static int Compare(Uri uri1, Uri uri2, UriComponents partsToCompare, UriFormat compareFormat, StringComparison comparisonType)
        {
#if !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5
            CoapStyleUriParser.Register();

            return Uri.Compare(uri1, uri2, partsToCompare, compareFormat, comparisonType);
#else
            // Setup Default ports before performing comparasons.
            if (_schemes.Contains(uri1.Scheme.ToLower()) && uri1.Port == -1)
                uri1 = new UriBuilder(uri1)
                {
                    Port = uri1.Scheme == "coap" 
                        ? Coap.Port 
                        : Coap.PortDTLS
                }.Uri;

            if (_schemes.Contains(uri2.Scheme.ToLower()) && uri2.Port == -1)
                uri2 = new UriBuilder(uri2)
                {
                    Port = uri2.Scheme == "coap"
                        ? Coap.Port
                        : Coap.PortDTLS
                }.Uri;

            return Uri.Compare(uri1, uri2, partsToCompare, compareFormat, comparisonType);
#endif
        }
    }
}