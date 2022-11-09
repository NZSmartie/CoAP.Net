using System;
using System.Collections.Generic;

namespace CoAPNet.Dtls.Server.Statistics
{
    public class DtlsSessionStatistics
    {
        public string EndPoint { get; set; }
        public IReadOnlyDictionary<string, object> ConnectionInfo { get; set; }
        public DateTime SessionStartTime { get; internal set; }
        public DateTime LastReceivedTime { get; internal set; }
    }
}
