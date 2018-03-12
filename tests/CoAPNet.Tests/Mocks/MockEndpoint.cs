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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoAPNet.Utils;
using System.Diagnostics;

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

        public virtual Task SendAsync(CoapPacket packet, CancellationToken token)
        {
            Debug.WriteLine($"Writing packet {{{string.Join(", ", packet.Payload)}}} {CoapMessage.CreateFromBytes(packet.Payload)}");
            return IsDisposed
                ? throw new CoapEndpointException("Encdpoint Disposed")
                : MockSendAsync(packet, token);
        }

        public virtual Task MockSendAsync(CoapPacket packet, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public void EnqueueReceivePacket(CoapPacket packet)
        {
            lock (_receiveQueue)
            {
                Debug.WriteLine($"MockEndpoint: Enqueing packet {{{string.Join(", ", packet.Payload)}}} {CoapMessage.CreateFromBytes(packet.Payload)}");
                _receiveQueue.Enqueue(packet);
            }
            _receiveEnqueuedEvent.Set();
        }

        public virtual Task<CoapPacket> ReceiveAsync(CancellationToken token)
        {
            return IsDisposed
                ? throw new CoapEndpointException("Encdpoint Disposed")
                : MockReceiveAsync(token);
        }

        public virtual async Task<CoapPacket> MockReceiveAsync(CancellationToken token)
        {
            await _receiveEnqueuedEvent.WaitAsync(token);
            if (IsDisposed)
                throw new CoapEndpointException("Encdpoint Disposed");

            CoapPacket packet;

            lock (_receiveQueue)
            {
                packet = _receiveQueue.Dequeue();
            }

            Debug.WriteLine($"MockEndpoint: Read packet {{{string.Join(", ", packet.Payload)}}}");
            return packet;
        }

        public string ToString(CoapEndpointStringFormat format)
        {
            return $"[ {nameof(MockEndpoint)} ]";
        }
    }
}