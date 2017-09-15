using System;
using System.Linq;

namespace CoAPNet.Utils
{
    // TODO: This helper class will be redundant in .Net Core 2.0 through sub-classing HttpStyleUriParser and adding CoAP defaults.
    public static class CoapUri
    {
        private static readonly string[] _schemes = {"coap", "coaps"};

        public static int Compare(Uri uri1, Uri uri2, UriComponents partsToCompare, UriFormat compareFormat, StringComparison comparisonType)
        {
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
        }
    }
}