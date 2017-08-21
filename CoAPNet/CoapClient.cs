using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoAPNet;
using CoAPNet.Utils;

namespace CoAPNet
{
    public class CoapMessageReceivedEventArgs : EventArgs
    {
        public CoapMessage Message { get; set; }
        public ICoapEndpoint Endpoint { get; set; }
    }

    public class CoapClientException : CoapException
    {
        public CoapClientException() : base() { }

        public CoapClientException(string message) : base(message) { }

        public CoapClientException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class CoapClient : IDisposable
    {
        public ICoapEndpoint Endpoint { get; private set; }

        private int _messageId;

        // I'm not particularly fond of the following _messageQueue and _messageResponses... Feels more like a hack. but it works? NEEDS MORE TESTING!!!
        private readonly ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>> _messageReponses 
            = new ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>>();


        public int MaxRetransmitAttempts { get; set; } = Coap.MaxRestransmitAttempts;

        public TimeSpan RetransmitTimeout { get; set; } = Coap.RetransmitTimeout;

        public CoapClient(ICoapEndpoint endpoint)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            _messageId = new Random().Next() & 0xFFFF;
        }

        private readonly Queue<Task<CoapReceiveResult>> _receiveQueue = new Queue<Task<CoapReceiveResult>>();
        private Task _receiveTask = Task.CompletedTask;
        private readonly AsyncAutoResetEvent _receiveEvent = new AsyncAutoResetEvent(false);

        public async Task<CoapReceiveResult> ReceiveAsync(CancellationToken token)
        {
            if (_receiveTask.IsCompleted)
                _receiveTask = ReceiveAsyncInternal();

            await _receiveEvent.WaitAsync(token);

            Task<CoapReceiveResult> resultTask;
            lock (_receiveQueue)
            {
                resultTask = _receiveQueue.Dequeue();
            }
            return await resultTask;
        }


        private async Task ReceiveAsyncInternal()
        {
            try
            {
                while (true)
                {
                    var payload = await Endpoint.ReceiveAsync(CancellationToken.None);
                    var message = new CoapMessage(Endpoint.IsMulticast);
                    try
                    {
                        message.Deserialise(payload.Payload);
                    }
                    catch (CoapMessageFormatException)
                    {
                        if (message.Type == CoapMessageType.Confirmable
                            && !Endpoint.IsMulticast)
                        {
                            await SendAsync(new CoapMessage
                            {
                                Id = message.Id,
                                Type = CoapMessageType.Reset
                            }, payload.Endpoint);
                        }
                        throw;
                    }

                    if (_messageReponses.ContainsKey(message.Id))
                        _messageReponses[message.Id].TrySetResult(message);

                    lock (_receiveQueue)
                    {
                        _receiveQueue.Enqueue(Task.FromResult(new CoapReceiveResult(payload.Endpoint, message)));
                    }
                    _receiveEvent.Set();
                }
            }
            catch (Exception ex)
            {
                if (Endpoint == null)
                    return;

                lock (_receiveQueue)
                {
                    // Gona cheat and enque that exception so it gets thrown as if this detached-infinite-loop never existed...
                    _receiveQueue.Enqueue(Task.FromException<CoapReceiveResult>(ex));
                }
                _receiveEvent.Set();
            }
        }

        public void Dispose()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            Endpoint.Dispose();
            Endpoint = null;
            _receiveTask?.Wait();
        }

        public async Task<CoapMessage> GetResponseAsync(int messageId)
        {
            if (!_messageReponses.TryGetValue(messageId, out var responseTask))
                throw new ArgumentOutOfRangeException($"The current message id ({messageId}) is not pending a due response");

            await responseTask.Task.ConfigureAwait(false);

            // TODO: if wait timed out, retry sending message with back-off delay
            _messageReponses.TryRemove(messageId, out responseTask);

            return responseTask.Task.GetAwaiter().GetResult();
        }

        public virtual async Task<int> SendAsync(CoapMessage message)
        {
            return await SendAsync(message, null, CancellationToken.None);
        }

        public virtual async Task<int> SendAsync(CoapMessage message, ICoapEndpoint endpoint)
        {
            return await SendAsync(message, endpoint, CancellationToken.None);
        }

        private int GetNextMessageId()
        {
            return Interlocked.Increment(ref _messageId) & ushort.MaxValue;
        }

        public virtual async Task<int> SendAsync(CoapMessage message, ICoapEndpoint endpoint, CancellationToken token)
        {
            if (message.Id == 0)
                message.Id = GetNextMessageId();

            if (message.Type != CoapMessageType.Confirmable)
            {
                await SendAsyncInternal(message, endpoint, token).ConfigureAwait(false);
                return message.Id;
            }

            _messageReponses.TryAdd(message.Id, new TaskCompletionSource<CoapMessage>());

            if (_receiveTask.IsCompleted)
                _receiveTask = ReceiveAsyncInternal();

            for (var attempt = 1; attempt <= MaxRetransmitAttempts; attempt++)
            {
                await SendAsyncInternal(message, endpoint, token).ConfigureAwait(false);
                await _receiveEvent.WaitAsync(TimeSpan.FromMilliseconds(RetransmitTimeout.TotalMilliseconds * attempt), token);
                if(_messageReponses[message.Id].Task.IsCompleted)
                    return message.Id;
            }
            throw new CoapClientException($"Max retransmission attempts reached for Message Id: {message.Id}");
        }

        private async Task SendAsyncInternal(CoapMessage message, ICoapEndpoint endpoint, CancellationToken token)
        {
            await Endpoint.SendAsync(new CoapPacket
            {
                Payload = message.Serialise(),
                MessageId = message.Id,
                Endpoint = endpoint
            }, token).ConfigureAwait(false);
        }

        internal void SetNetMessageId(int value)
        {
            Interlocked.Exchange(ref _messageId, value - 1);
        }
        
        #region Request Operations

        public virtual async Task<int> GetAsync(string uri, ICoapEndpoint endpoint = null, CancellationToken token = default(CancellationToken))
        {
            var message = new CoapMessage
            {
                Id = GetNextMessageId(),
                Code = CoapMessageCode.Get,
                Type = CoapMessageType.Confirmable
            };
            message.FromUri(uri);

            _messageReponses.TryAdd(message.Id, new TaskCompletionSource<CoapMessage>());

            return await SendAsync(message, endpoint, token).ConfigureAwait(false);
        }

        public virtual Task<int> PutAsync(string uri, CoapMessage message, ICoapEndpoint endpoint = null)
        {
            throw new NotImplementedException();
        }
        
        public virtual Task<int> PostAsync(string uri, CoapMessage message, ICoapEndpoint endpoint = null)
        {
            throw new NotImplementedException();
        }
        
        public virtual Task<int> DeleteAsync(string uri, ICoapEndpoint endpoint = null)
        {
            throw new NotImplementedException();
        }
        
        public virtual Task<int> ObserveAsync(string uri, ICoapEndpoint endpoint = null)
        {
            throw new NotImplementedException();
        }

        #endregion


    }

    public class CoapReceiveResult
    {
        public CoapReceiveResult(ICoapEndpoint endpoint, CoapMessage message)
        {
            Endpoint = endpoint;

            Message = message;
        }

        public ICoapEndpoint Endpoint { get; }

        public CoapMessage Message { get; }
    }
}
