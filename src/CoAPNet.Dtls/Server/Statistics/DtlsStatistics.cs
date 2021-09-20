using System.Collections.Generic;

namespace CoAPNet.Dtls.Server.Statistics
{
    public class DtlsStatistics
    {
        public IReadOnlyList<DtlsSessionStatistics> Sessions { get; internal set; }
    }
}
