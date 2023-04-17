using System;
using CoAPNet.Dtls.Server.Statistics;
using Microsoft.Extensions.Logging;

namespace CoAPNet.Dtls.Server
{
    public class CoapDtlsServerTransportFactory : ICoapTransportFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDtlsServerFactory _tlsServerFactory;
        private readonly TimeSpan _sessionTimeout;
        private CoapDtlsServerTransport _transport;

        /// <param name="loggerFactory">LoggerFactory to use for transport logging</param>
        /// <param name="tlsServerFactory">a <see cref="IDtlsServerFactory"/> that creates the DtlsServer to use.</param>
        /// <param name="sessionTimeout"> The time without new packets after which a session is assumed to be stale and closed</param>
        public CoapDtlsServerTransportFactory(ILoggerFactory loggerFactory, IDtlsServerFactory tlsServerFactory, TimeSpan sessionTimeout)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _tlsServerFactory = tlsServerFactory ?? throw new ArgumentNullException(nameof(tlsServerFactory));
            _sessionTimeout = sessionTimeout;
        }

        public ICoapTransport Create(ICoapEndpoint endPoint, ICoapHandler handler)
        {
            var serverEndpoint = endPoint as CoapDtlsServerEndPoint;
            if (serverEndpoint == null)
                throw new ArgumentException($"Endpoint has to be {nameof(CoapDtlsServerEndPoint)}");
            if (_transport != null)
                throw new InvalidOperationException("CoAP transport may only be created once!");

            _transport = new CoapDtlsServerTransport(serverEndpoint, handler, _tlsServerFactory, _loggerFactory.CreateLogger<CoapDtlsServerTransport>(), _sessionTimeout);
            return _transport;
        }

        public DtlsStatistics GetTransportStatistics()
        {
            return _transport?.GetStatistics();
        }
    }
}
