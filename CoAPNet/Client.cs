using System;
using System.Collections.Concurrent;
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

    public class CoapClient : IDisposable
    {
        protected  ICoapEndpoint Endpoint;

        private ushort _messageId;

        // I'm not particularly fond of the following _messageQueue and _messageResponses... Feels more like a hack. but it works? NEEDS MORE TESTING!!!
        private readonly ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>> _messageReponses 
            = new ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>>();

        private CancellationTokenSource _receiveCancellationToken;

        public event EventHandler<CoapMessageReceivedEventArgs> OnMessageReceived;

        public event EventHandler<EventArgs> OnClosed;

        public CoapClient(ICoapEndpoint endpoint)
        {
            Endpoint = endpoint;

            _messageId = (ushort)(new Random().Next() & 0xFFFFu);
        }

        public bool IsListening => _receiveCancellationToken?.IsCancellationRequested ?? false;

        public void Listen() {
            if (IsListening)
                return;

            _receiveCancellationToken = new CancellationTokenSource();

            Task.Factory.StartNew(() =>
            {
                var token = _receiveCancellationToken.Token;

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var payload = Endpoint.ReceiveAsync();
                        payload.Wait(token);
                        if (!payload.IsCompleted || payload.Result == null)
                            continue;

                        var message = new CoapMessage(Endpoint.IsMulticast);
                        try
                        {
                            message.Deserialise(payload.Result.Payload);
                        }
                        catch(CoapMessageFormatException)
                        {
                            if (message.Type == CoapMessageType.Confirmable 
                                && !Endpoint.IsMulticast)
                            {
                                Task.Run(() => SendAsync(new CoapMessage
                                {
                                    Id = message.Id,
                                    Type = CoapMessageType.Reset
                                }, payload.Result.Endpoint));
                            }
                            continue;
                        }

                        if (_messageReponses.ContainsKey(message.Id))
                            _messageReponses[message.Id].TrySetResult(message);

                        OnMessageReceived?.Invoke(this, new CoapMessageReceivedEventArgs
                        {
                            Message = message,
                            Endpoint = payload.Result.Endpoint
                        });
                    }
                    catch(CoapEndpointException)
                    {
                        _receiveCancellationToken.Cancel();
                    }
                }
                OnClosed?.Invoke(this, new EventArgs());
            });
        }

        public void Dispose()
        {
            // Cancels our receiver task
            _receiveCancellationToken?.Cancel();
        }

        public async Task<CoapMessage> GetResponseAsync(int messageId)
        {
            TaskCompletionSource<CoapMessage> responseTask = null;
            if (!_messageReponses.TryGetValue(messageId, out responseTask))
                throw new ArgumentOutOfRangeException("Message.Id is not pending response");

            await responseTask.Task;

            // ToDo: if wait timed out, retry sending message with back-off delay
            _messageReponses.TryRemove(messageId, out responseTask);

            return responseTask.Task.Result;
        }

        public async Task<int> SendAsync(CoapMessage message, ICoapEndpoint endpoint = null)
        {
            if (message.Id == 0)
                message.Id = _messageId++;

            if(message.Type == CoapMessageType.Confirmable)
                _messageReponses.TryAdd(message.Id, new TaskCompletionSource<CoapMessage>());

            await Endpoint.SendAsync(new CoapPayload { Payload = message.Serialise(), MessageId = message.Id, Endpoint = endpoint });

            return message.Id;
        }

        public async Task<int> GetAsync(string uri, ICoapEndpoint endpoint = null)
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
