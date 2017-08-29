using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoAPNet.Utils;

namespace CoAPNet.Tests.Mocks
{
    public class MockEndpoint : ICoapEndpoint
    {
        public virtual bool IsSecure { get; } = false;
        public virtual bool IsMulticast { get; } = false;
        public virtual Uri BaseUri { get; } = new Uri("coap://localhost/");

        internal bool IsDisposed = false;

        private readonly Queue<CoapPacket> _receiveQueue = new Queue<CoapPacket>();
        private readonly AsyncAutoResetEvent _receiveEnqueuedEvent = new AsyncAutoResetEvent(false);

        public void Dispose()
        {
            IsDisposed = true;
            _receiveEnqueuedEvent.Set();
        }

        public virtual Task SendAsync(CoapPacket packet)
        {
            return IsDisposed
                ? throw new CoapEndpointException("Encdpoint Disposed")
                : MockSendAsync(packet);
        }

        public virtual Task MockSendAsync(CoapPacket packet)
        {
            return Task.CompletedTask;
        }

        public void EnqueueReceivePacket(CoapPacket packet)
        {
            lock (_receiveQueue)
            {
                _receiveQueue.Enqueue(packet);
            }
            _receiveEnqueuedEvent.Set();
        }

        public virtual Task<CoapPacket> ReceiveAsync()
        {
            return IsDisposed
                ? throw new CoapEndpointException("Encdpoint Disposed")
                : MockReceiveAsync();
        }

        public virtual async Task<CoapPacket> MockReceiveAsync()
        {
            await _receiveEnqueuedEvent.WaitAsync(CancellationToken.None);
            if (IsDisposed)
                throw new CoapEndpointException("Encdpoint Disposed");

            CoapPacket packet;
            lock (_receiveQueue)
            {
                packet = _receiveQueue.Dequeue();
            }
            return packet;
        }
    }
}