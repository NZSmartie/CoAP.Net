using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoAPNet
{
    public class CoapServer
    {
        private readonly ICoapTransportFactory _transportFactory;

        private readonly ConcurrentBag<ICoapTransport> _transports = new ConcurrentBag<ICoapTransport>();

        private Queue<ICoapEndpoint> _bindToQueue = new Queue<ICoapEndpoint>();

        public CoapServer(ICoapTransportFactory transportFactory)
        {
            _transportFactory = transportFactory;
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

            var bindToQueue = Interlocked.Exchange(ref _bindToQueue, null);
            while (bindToQueue.Count > 0)
                await BindToNextendpoint(bindToQueue.Dequeue(), handler);

            // TODO: Implement MaxRequests
        }

        public async Task StopAsync(CancellationToken token)
        {
            if(Interlocked.CompareExchange(ref _serverState, (int)ServerState.Stopped, (int)ServerState.Started) != (int)ServerState.Started)
                throw new InvalidOperationException($"Unable to stop {nameof(CoapServer)} not in started state");

            while (_transports.TryTake(out var transport))
            {
                await transport.StopAsync();
                await transport.UnbindAsync();
            }
        }

        private async Task BindToNextendpoint(ICoapEndpoint endpoint, ICoapHandler handler)
        {
            var transport = _transportFactory.Create(endpoint, handler);

            await transport.BindAsync();

            _transports.Add(transport);
        }
    }
}