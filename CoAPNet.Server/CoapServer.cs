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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CoAPNet.Server
{
    public class CoapServer
    {
        private readonly ICoapTransportFactory _transportFactory;
        private readonly ILogger<CoapServer> _logger;

        private readonly ConcurrentBag<ICoapTransport> _transports = new ConcurrentBag<ICoapTransport>();

        private Queue<ICoapEndpoint> _bindToQueue = new Queue<ICoapEndpoint>();

        public CoapServer(ICoapTransportFactory transportFactory, ILogger<CoapServer> logger = null)
        {
            _transportFactory = transportFactory;
            _logger = logger;
        }

        public Task BindTo(ICoapEndpoint endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));
            if(_serverState != (int)ServerState.None || _bindToQueue == null)
                throw new InvalidOperationException("Can not bind to endpoint when server has started");

            _bindToQueue?.Enqueue(endpoint);

            return Task.CompletedTask;
        }

        private enum ServerState {None = 0, Started = 1, Stopped = 2};
        private int _serverState = (int)ServerState.None;

        public async Task StartAsync(ICoapHandler handler, CancellationToken token)
        {
            if(Interlocked.CompareExchange(ref _serverState, (int)ServerState.Started, (int)ServerState.None) != (int)ServerState.None)
                throw new InvalidOperationException($"{nameof(CoapServer)} has already started");

            _logger?.LogDebug(CoapLoggingEvents.ServerStart, "Starting");

            var bindToQueue = Interlocked.Exchange(ref _bindToQueue, null);
            while (bindToQueue.Count > 0)
                await BindToNextendpoint(bindToQueue.Dequeue(), handler);

            // TODO: Implement MaxRequests
            _logger?.LogInformation(CoapLoggingEvents.ServerStart, "Started");
        }

        public async Task StopAsync(CancellationToken token)
        {
            if(Interlocked.CompareExchange(ref _serverState, (int)ServerState.Stopped, (int)ServerState.Started) != (int)ServerState.Started)
                throw new InvalidOperationException($"Unable to stop {nameof(CoapServer)} not in started state");

            _logger?.LogDebug(CoapLoggingEvents.ServerStop, "Stopping");

            while (_transports.TryTake(out var transport))
            {
                await transport.StopAsync();
                await transport.UnbindAsync();
            }

            _logger?.LogInformation(CoapLoggingEvents.ServerStop, "Stopped");
        }

        private async Task BindToNextendpoint(ICoapEndpoint endpoint, ICoapHandler handler)
        {
            _logger?.LogDebug(CoapLoggingEvents.ServerBindTo, "Binding to", endpoint);
            var transport = _transportFactory.Create(endpoint, handler);

            await transport.BindAsync();

            _transports.Add(transport);
        }
    }
}