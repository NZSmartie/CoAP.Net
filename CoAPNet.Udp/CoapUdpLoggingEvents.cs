namespace CoAPNet.Udp
{
    public static class CoapUdpLoggingEvents
    {
        public const int TransportBase         = 1000;
        public const int TransportBind         = TransportBase + 1;
        public const int TransportRequestsLoop = TransportBase + 2;
    }
}