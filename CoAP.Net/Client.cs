using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CoAP.Net
{
    public class CoapMessageReceivedEventArgs : EventArgs
    {
        public CoapMessage Message { get; set; }
        public ICoapEndpoint Endpoint { get; set; }
    }

    public class CoapEndpointException : Exception { }

    public class CoapClient : IDisposable
    {
        private ICoapEndpoint _transport;
        private ushort _messageId;

        // I'm not particularly fond of the following _messageQueue and _messageResponses... Feels more like a hack. but it works? NEEDS MORE TESTING!!!
        private ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>> _messageReponses 
            = new ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>>();

        private CancellationTokenSource _receiveCancellationToken;

        public event EventHandler<CoapMessageReceivedEventArgs> OnMessageReceived;

        public CoapClient(ICoapEndpoint transport)
        {
            _transport = transport;

            _messageId = (ushort)(new Random().Next() & 0xFFFFu);
        }

        public bool IsListening { get => _receiveCancellationToken != null && !_receiveCancellationToken.IsCancellationRequested; }

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
                        var payload = _transport.ReceiveAsync();
                        payload.Wait(token);
                        if (!payload.IsCompleted || payload.Result == null)
                            continue;

                        var message = new CoapMessage();
                        try
                        {
                            message.Deserialise(payload.Result.Payload);
                        }
                        catch(CoapMessageFormatException fe)
                        {
                            if (message.Type == CoapMessageType.Confirmable)
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
                        else
                            OnMessageReceived?.Invoke(this, new CoapMessageReceivedEventArgs
                            {
                                Message = message,
                                Endpoint = payload.Result.Endpoint
                            });
                    }
                    catch(CoapEndpointException ex)
                    {
                        _receiveCancellationToken.Cancel();
                    }
                }
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

            await _transport.SendAsync(new CoapPayload { Payload = message.Serialise(), MessageId = message.Id, Endpoint = endpoint });

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
