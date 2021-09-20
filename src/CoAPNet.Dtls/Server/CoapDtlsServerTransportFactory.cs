using System;
using CoAPNet.Dtls.Server.Statistics;
using Microsoft.Extensions.Logging;

namespace CoAPNet.Dtls.Server
{
    public class CoapDtlsServerTransportFactory : ICoapTransportFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDtlsServerFactory _tlsServerFactory;
        private CoapDtlsServerTransport _transport;

        /// <param name="loggerFactory">LoggerFactory to use for transport logging</param>
        /// <param name="dtlsStatisticsStore">a <see cref="DtlsStatisticsStore"/> to store connection statistics in.</param>
        public CoapDtlsServerTransportFactory(ILoggerFactory loggerFactory, IDtlsServerFactory tlsServerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _tlsServerFactory = tlsServerFactory ?? throw new ArgumentNullException(nameof(tlsServerFactory));
        }

        public ICoapTransport Create(ICoapEndpoint endPoint, ICoapHandler handler)
        {
            var serverEndpoint = endPoint as CoapDtlsServerEndPoint;
            if (serverEndpoint == null)
                throw new ArgumentException($"Endpoint has to be {nameof(CoapDtlsServerEndPoint)}");
            if (_transport != null)
                throw new InvalidOperationException("CoAP transport may only be created once!");

            _transport = new CoapDtlsServerTransport(serverEndpoint, handler, _tlsServerFactory, _loggerFactory.CreateLogger<CoapDtlsServerTransport>());
            return _transport;
        }

        public DtlsStatistics GetTransportStatistics()
        {
            return _transport?.GetStatistics();
        }
    }
}
