using System.Collections.Generic;

namespace CoAPNet.Dtls.Server.Statistics
{
    public class DtlsStatistics
    {
        public IReadOnlyList<DtlsSessionStatistics> Sessions { get; internal set; }
        public uint HandshakeSuccessCount { get; internal set; }
        public uint HandshakeErrorCount { get; internal set; }
        public uint PacketsReceivedSessionUnknown { get; internal set; }
        public uint PacketsReceivedSessionByEp { get; internal set; }
        public uint PacketsReceivedSessionByCid { get; internal set; }
        public uint PacketsSent { get; internal set; }
    }
}
