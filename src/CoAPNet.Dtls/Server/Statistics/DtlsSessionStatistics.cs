using System;

namespace CoAPNet.Dtls.Server.Statistics
{
    public class DtlsSessionStatistics
    {
        public string EndPoint { get; set; }
        public string ConnectionInfo { get; set; }
        public DateTime SessionStartTime { get; internal set; }
        public DateTime LastReceivedTime { get; internal set; }
    }
}
