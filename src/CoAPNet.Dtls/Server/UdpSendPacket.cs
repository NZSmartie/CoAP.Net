using System.Net;

namespace CoAPNet.Dtls.Server
{
    class UdpSendPacket
    {
        public UdpSendPacket(byte[] payload, IPEndPoint targetEndPoint)
        {
            Payload = payload;
            TargetEndPoint = targetEndPoint;
        }

        public byte[] Payload { get; }
        public IPEndPoint TargetEndPoint { get; }
    }
}
