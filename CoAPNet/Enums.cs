using System;

namespace CoAPNet
{
    public static class RegisteredOptionNumber
    {
        public const int IfMatch = 1;
        public const int UriHost = 3;
        public const int ETag = 4;
        public const int IfNoneMatch = 5;
        public const int UriPort = 7;
        public const int LocationPath = 8;
        public const int UriPath = 11;
        public const int ContentFormat = 12;
        public const int MaxAge = 14;
        public const int UriQuery = 15;
        public const int Accept = 17;
        public const int LocationQuery = 20;
        public const int ProxyUri = 35;
        public const int ProxyScheme = 39;
        public const int Size1 = 60;
    }

    public static class Coap
    {
        public const string MulticastIPv4 = "224.0.1.187";
        public const string MulticastIPv6 = "FF0X::FD";
        public const ushort Port = 5683;
        public const ushort PortDTLS = 5684;

        public const int MaxRestransmitAttempts = 3;
        public static readonly TimeSpan RetransmitTimeout = TimeSpan.FromSeconds(2);

        public static string GetMulticastIPv6ForScope(int scope)
        {
            if(scope < 1 || scope >= 15)
                throw new ArgumentOutOfRangeException(nameof(scope), "Scope is in the range from 1 to 14. 0 and 15 are reserved. (See RFC 7346)");
            return MulticastIPv6.Replace("X", scope.ToString("X"));
        }
    }
}
