#region License
// Copyright 2017 Roman Vaughan (NZSmartie)
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CoAPNet.Udp
{
    public class CoapUdpTransportFactory : ICoapTransportFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public CoapUdpTransportFactory(ILoggerFactory loggerFactory = null)
        {
            _loggerFactory = loggerFactory;
        }

        public ICoapTransport Create(ICoapEndpoint endPoint, ICoapHandler handler)
        {
            return new CoapUdpTransport(endPoint as CoapUdpEndPoint ?? throw new InvalidOperationException(), handler, _loggerFactory?.CreateLogger<CoapUdpTransport>());
        }
    }

    public class CoapUdpTransport : ICoapTransport
    {
        private CoapUdpEndPoint _endPoint;

        private readonly ICoapHandler _coapHandler;
        private readonly ILogger<CoapUdpTransport> _logger;

        private Task _listenTask;
        private readonly CancellationTokenSource _listenTaskCTS = new CancellationTokenSource();

        public CoapUdpTransport(CoapUdpEndPoint endPoint, ICoapHandler coapHandler, ILogger<CoapUdpTransport> logger = null)
        {
            _endPoint = endPoint;
            _coapHandler = coapHandler;
            _logger = logger;
        }

        public async Task BindAsync()
        {
            if(_listenTask != null)
                throw new InvalidOperationException($"{nameof(CoapUdpTransport)} has already started");

            try
            {
                _logger?.LogDebug(CoapUdpLoggingEvents.TransportBind, $"Binding to {_endPoint}");
                await _endPoint.BindAsync().ConfigureAwait(false);

                _logger?.LogDebug(CoapUdpLoggingEvents.TransportBind, "Creating long running task");
                _listenTask = RunRequestsLoopAsync();
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                throw new Exception($"Can not bind to enpoint as address may already be in use. {e.Message}", e);
            }
        }

        public async Task UnbindAsync()
        {
            if (_endPoint == null)
                return;

            var endPoint = _endPoint;
            _endPoint = null;

            endPoint.Dispose();
            try
            {
                _listenTaskCTS.Cancel();
                await _listenTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            { }

            _listenTask = null;
        }

        public Task StopAsync()
        {
            // TODO: Cancellation token to stop RunRequestsLoopAsync
            return Task.CompletedTask;
        }

        private async Task RunRequestsLoopAsync()
        {
            try
            {
                while (true)
                {
                    var request = await _endPoint.ReceiveAsync(_listenTaskCTS.Token);
                    _logger?.LogDebug(CoapUdpLoggingEvents.TransportRequestsLoop, "Received message");

                    _ = ProcessRequestAsync(new CoapConnectionInformation
                    {
                        LocalEndpoint = _endPoint,
                        RemoteEndpoint = request.Endpoint,
                    }, request.Payload);
                }
            }
            catch (Exception)
            {
                _logger?.LogInformation(CoapUdpLoggingEvents.TransportRequestsLoop, "Shutting down");

                if (_endPoint != null)
                    throw;
                }
        }

        private async Task ProcessRequestAsync(ICoapConnectionInformation connection, byte[] payload)
        {
            try
            {
                await _coapHandler.ProcessRequestAsync(connection, payload);
            }
            catch (Exception ex)
            {
                _logger?.LogError(CoapUdpLoggingEvents.TransportRequestsLoop, ex , "Unexpected exception was thrown");
                throw;
            }
        }
    }
}