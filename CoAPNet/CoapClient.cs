using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoAPNet;
using CoAPNet.Utils;

namespace CoAPNet
{
    [ExcludeFromCodeCoverage]
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
        private readonly ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>> _messageResponses 
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
            Task<CoapReceiveResult> resultTask = null;
            lock (_receiveQueue)
            {
                if (_receiveQueue.Count > 0)
                {
                    resultTask = _receiveQueue.Dequeue();
                }
            }
            if (resultTask != null)
                return resultTask.IsCanceled
                    ? null
                    : await resultTask;
            
            if (Endpoint == null)
                throw new InvalidOperationException($"{nameof(CoapClient)} is in an invalid state");

            StartReceiveAsyncInternal();

            await _receiveEvent.WaitAsync(token);

            if (token.IsCancellationRequested)
                return null;

            lock (_receiveQueue)
            {
                resultTask = _receiveQueue.Dequeue();
            }

            return await resultTask;
        }

        private readonly object _receiveTaskLock = new object();
        private void StartReceiveAsyncInternal()
        {
            lock (_receiveTaskLock)
            {
                if (!_receiveTask.IsCompleted)
                    return;

                var task = Task.Factory.StartNew(
                    ReceiveAsyncInternal,
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);


                _receiveTask = task.Unwrap();
            }
        }

        private async Task ReceiveAsyncInternal()
        {
            try
            {
                while (true)
                {
                    if (Endpoint == null)
                        return;

                    var payload = await Endpoint.ReceiveAsync();

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

                    if (_messageResponses.ContainsKey(message.Id))
                    {
                        _messageResponses[message.Id].TrySetResult(message);
                    }

                    lock (_receiveQueue)
                    {
                        _receiveQueue.Enqueue(Task.FromResult(new CoapReceiveResult(payload.Endpoint, message)));
                    }
                    _receiveEvent.Set();
                }
            }
            catch (Exception ex)
            {
                if (ex is CoapEndpointException)
                {
                    Endpoint?.Dispose();
                    Endpoint = null;

                    foreach (var response in _messageResponses.Values)
                        response.TrySetCanceled();
                }

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
            var endpoint = Endpoint;
            Endpoint = null;

            endpoint?.Dispose();

            if (!_receiveTask.IsCompleted && !_receiveTask.IsCanceled && !_receiveTask.IsFaulted && !_receiveTask.Wait(5000))
                throw new CoapClientException($"Took too long to dispose of the enclosed {nameof(Endpoint)}");
        }

        public async Task<CoapMessage> GetResponseAsync(int messageId, CancellationToken token = default(CancellationToken))
        {
            TaskCompletionSource<CoapMessage> responseTask = null;

            if (!_messageResponses.TryGetValue(messageId, out responseTask))
                throw new ArgumentOutOfRangeException($"The current message id ({messageId}) is not pending a due response");

            if (responseTask.Task.IsCompleted)
                return responseTask.Task.Result;

            if (Endpoint == null)
                throw new InvalidOperationException($"{nameof(CoapClient)} is in an invalid state");

            for (var attempt = 1; attempt <= MaxRetransmitAttempts; attempt++)
            {
                StartReceiveAsyncInternal();

                var timeout = TimeSpan.FromMilliseconds(RetransmitTimeout.TotalMilliseconds * attempt);

                await Task.WhenAny(responseTask.Task, Task.Delay(timeout, token)).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                if (responseTask.Task.IsCompleted)
                    break;
            }

            if (responseTask.Task.Status != TaskStatus.RanToCompletion)
                throw new CoapClientException($"Max timeout reached for Message Id: {messageId}");

            Debug.Assert(_messageResponses.TryRemove(messageId, out responseTask));

            return responseTask.Task.Result;
        }

        public virtual async Task<int> SendAsync(CoapMessage message) => 
            await SendAsync(message, null, CancellationToken.None);

        public virtual async Task<int> SendAsync(CoapMessage message, CancellationToken token) =>
            await SendAsync(message, null, token);

        public virtual async Task<int> SendAsync(CoapMessage message, ICoapEndpoint endpoint) =>
            await SendAsync(message, endpoint, CancellationToken.None);

        private int GetNextMessageId() => 
            Interlocked.Increment(ref _messageId) & ushort.MaxValue;

        public virtual async Task<int> SendAsync(CoapMessage message, ICoapEndpoint endpoint, CancellationToken token)
        {
            if(Endpoint==null)
                throw new InvalidOperationException($"{nameof(CoapClient)} is in an invalid state");

            if (message.Id == 0)
                message.Id = GetNextMessageId();

            if (message.Type != CoapMessageType.Confirmable)
            {
                await SendAsyncInternal(message, endpoint, token).ConfigureAwait(false);
                return message.Id;
            }

            _messageResponses.TryAdd(message.Id, new TaskCompletionSource<CoapMessage>(TaskCreationOptions.RunContinuationsAsynchronously));
            var responseTaskSource = _messageResponses[message.Id];

            for (var attempt = 1; attempt <= MaxRetransmitAttempts; attempt++)
            {
                StartReceiveAsyncInternal();

                await SendAsyncInternal(message, endpoint, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                var timeout = TimeSpan.FromMilliseconds(RetransmitTimeout.TotalMilliseconds * attempt);

                await Task.WhenAny(responseTaskSource.Task, Task.Delay(timeout, token)).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();


                if (responseTaskSource.Task.IsCompleted)
                    return message.Id;
            }
            throw new CoapClientException($"Max retransmission attempts reached for Message Id: {message.Id}");
        }

        private async Task SendAsyncInternal(CoapMessage message, ICoapEndpoint remoteEndpoint, CancellationToken token)
        {
            if(Endpoint == null)
                return;
            
            if (remoteEndpoint == null)
                remoteEndpoint = new CoapEndpoint
                {
                    BaseUri = new UriBuilder(message.GetUri()) {Path = "/", Fragment = "", Query = ""}.Uri
                };

            await Task.Run(async () => await Endpoint.SendAsync(new CoapPacket
            {
                Payload = message.Serialise(),
                Endpoint = remoteEndpoint
            }), token).ConfigureAwait(false);
        }

        internal void SetNextMessageId(int value)
        {
            Interlocked.Exchange(ref _messageId, value - 1);
        }
        
        #region Request Operations

        public virtual async Task<int> GetAsync(string uri, CancellationToken token = default(CancellationToken))
        {
            return await GetAsync(uri, null, token);
        }

        public virtual async Task<int> GetAsync(string uri, ICoapEndpoint endpoint, CancellationToken token = default(CancellationToken))
        {
            var message = new CoapMessage
            {
                Code = CoapMessageCode.Get,
                Type = CoapMessageType.Confirmable
            };
            message.FromUri(uri);

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
