using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoAPNet
{
    public class CoapMessageReceivedEventArgs : EventArgs
    {
        public CoapMessage Message { get; set; }
        public ICoapEndpoint Endpoint { get; set; }
    }

    public class CoapEndpointException : Exception {
        public CoapEndpointException() :base() { }

        public CoapEndpointException(string message) : base(message) { }

        public CoapEndpointException(string message, Exception innerException) : base(message, innerException) { }
    }

    public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);

    public class CoapClient : IDisposable
    {
        public ICoapEndpoint Endpoint { get; }

        private ushort _messageId;

        // I'm not particularly fond of the following _messageQueue and _messageResponses... Feels more like a hack. but it works? NEEDS MORE TESTING!!!
        private readonly ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>> _messageReponses 
            = new ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>>();

        private CancellationTokenSource _receiveCancellationToken;

        public virtual event AsyncEventHandler<CoapMessageReceivedEventArgs> OnMessageReceived;

        public virtual event EventHandler<EventArgs> OnClosed;

        public CoapClient(ICoapEndpoint endpoint)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            _messageId = (ushort)(new Random().Next() & 0xFFFFu);
        }

        public virtual bool IsListening => _receiveCancellationToken?.IsCancellationRequested ?? false;

        public virtual void Listen() {
            if (IsListening)
                return;

            _receiveCancellationToken = new CancellationTokenSource();

            Task.Factory.StartNew(() =>
            {
                Task.WhenAll(ReceiveAsyncTasks()).Wait();

                OnClosed?.Invoke(this, new EventArgs());
            });
        }

        private IEnumerable<Task> ReceiveAsyncTasks()
        {
            var token = _receiveCancellationToken.Token;

            while (!token.IsCancellationRequested)
            {
                var receiveTask = Endpoint.ReceiveAsync();

                yield return receiveTask.ContinueWith(async payloadTask =>
                {
                    var payload = payloadTask.Result ?? throw new NullReferenceException();
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
                        return;
                    }

                    if (_messageReponses.ContainsKey(message.Id))
                        _messageReponses[message.Id].TrySetResult(message);

                    await InvokeOnMessageReceivedAsync(message, payload.Endpoint).ConfigureAwait(false);
                }, token);

                try
                {
                    receiveTask.Wait(token);
                }
                catch (CoapEndpointException)
                {
                    _receiveCancellationToken.Cancel();
                }
            }
        }

        private async Task InvokeOnMessageReceivedAsync(CoapMessage message, ICoapEndpoint endpoint, params Type[] acceptableExceptions)
        {
            try
            {
                if (OnMessageReceived == null)
                    return;
                foreach (var listener in OnMessageReceived.GetInvocationList())
                {
                    if (listener.DynamicInvoke(this, new CoapMessageReceivedEventArgs
                    {
                        Message = message,
                        Endpoint = endpoint
                    }) is Task task)
                        await task;
                }
            }
            catch (Exception ex)
            {
                if (!acceptableExceptions.Contains(ex.GetType()))
                    throw;
            }
        }

        public void Dispose()
        {
            // Cancels our receiver task
            _receiveCancellationToken?.Cancel();
        }

        public async Task<CoapMessage> GetResponseAsync(int messageId)
        {
            if (!_messageReponses.TryGetValue(messageId, out var responseTask))
                throw new ArgumentOutOfRangeException($"The current message id ({messageId}) is not pending a due response");

            await responseTask.Task;

            // ToDo: if wait timed out, retry sending message with back-off delay
            _messageReponses.TryRemove(messageId, out responseTask);

            return responseTask.Task.Result;
        }

        public virtual async Task<int> SendAsync(CoapMessage message, ICoapEndpoint endpoint = null)
        {
            if (message.Id == 0)
                message.Id = _messageId++;

            if(message.Type == CoapMessageType.Confirmable)
                _messageReponses.TryAdd(message.Id, new TaskCompletionSource<CoapMessage>());

            await Endpoint.SendAsync(new CoapPayload { Payload = message.Serialise(), MessageId = message.Id, Endpoint = endpoint });

            return message.Id;
        }

        public virtual async Task<int> GetAsync(string uri, ICoapEndpoint endpoint = null)
        {
            var message = new CoapMessage
            {
                Id = _messageId++,
                Code = CoapMessageCode.Get,
                Type = CoapMessageType.Confirmable
            };
            message.FromUri(uri);

            _messageReponses.TryAdd(message.Id, new TaskCompletionSource<CoapMessage>());

            return await SendAsync(message, endpoint);
        }
    }
}
