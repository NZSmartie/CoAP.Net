namespace CoAPNet.Server
{
    public static class CoapLoggingEvents
    {
        // CoapServer
        public const int ServerBase    = 1000;
        public const int ServerStart   = ServerBase + 1;
        public const int ServerStop    = ServerBase + 2;
        public const int ServerBindTo  = ServerBase + 3;

        // CoapHandler
        public const int HandlerBase           = 2000;
        public const int HandlerProcessRequest = HandlerBase + 1;
    }
}