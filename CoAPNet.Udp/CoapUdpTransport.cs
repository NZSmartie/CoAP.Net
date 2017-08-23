using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CoAPNet.Udp
{
    public class CoapUdpTransportFactory : ICoapTransportFactory
    {
        public ICoapTransport Create(ICoapEndpoint endPoint, ICoapHandler handler)
        {
            return new CoapUdpTransport(endPoint as CoapUdpEndPoint ?? throw new InvalidOperationException(), handler);
        }
    }

    public class CoapUdpTransport : ICoapTransport
    {
        private CoapUdpEndPoint _endPoint;

        private readonly ICoapHandler _coapHandler;

        private Task _listenTask;

        public CoapUdpTransport(CoapUdpEndPoint endPoint, ICoapHandler coapHandler)
        {
            _endPoint = endPoint;
            _coapHandler = coapHandler;
        }

        public async Task BindAsync()
        {
            if(_listenTask != null)
                throw new InvalidOperationException($"{nameof(CoapUdpTransport)} has already started");

            try
            {
                await _endPoint.BindAsync().ConfigureAwait(false);

                _listenTask = RunRequestsLoopAsync();
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                throw new Exception($"Can not bind to enpoint as address is already in use. {e.Message}", e);
            }
        }

        public async Task UnbindAsync()
        {
            if (_endPoint == null)
                return;

            var endPoint = _endPoint;
            _endPoint = null;

            endPoint.Dispose();
            await _listenTask.ConfigureAwait(false);
            _listenTask = null;
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }

        private async Task RunRequestsLoopAsync()
        {
            try
            {
                while (true)
                {
                    var request = await _endPoint.ReceiveAsync();
                    _ = _coapHandler.ProcessRequestAsync(new CoapConnectionInformation
                    {
                        LocalEndpoint = _endPoint,
                        RemoteEndpoint = request.Endpoint,
                    }, request.Payload);
                }
            }
            catch (Exception)
            {
                if (_endPoint != null)
                    throw;
            }
        }
    }
}