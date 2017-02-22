using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CoAP.Net
{
    public class CoapClient : IDisposable
    {
        private ICoapEndpoint _transport;
        private ushort _messageId;

        // I'm not particularly fond of the following _messageQueue and _messageResponses... Feels more like a hack. but it works? NEEDS MORE TESTING!!!
        private ConcurrentDictionary<int, CoapMessage> _messageQueue = new ConcurrentDictionary<int, CoapMessage>();
        private ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>> _messageReponses = new ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>>();

        private CancellationTokenSource _receiveCancellationToken;

        public CoapClient(ICoapEndpoint transport)
        {
            _transport = transport;

            _messageId = (ushort)(new Random().Next() & 0xFFFFu);

            _receiveCancellationToken = new CancellationTokenSource();

            Task.Factory.StartNew(() =>
            {
                var token = _receiveCancellationToken.Token;
                while (!token.IsCancellationRequested)
                {
                    var payload = _transport.ReceiveAsync();
                    payload.Wait(token);
                    if (!payload.IsCompleted || payload.Result == null)
                        continue;

                    var message = new CoapMessage();

                    message.Deserialise(payload.Result.Payload);

                    _messageQueue.TryAdd(message.Id, message);
                    if (_messageReponses.ContainsKey(message.Id))
                        _messageReponses[message.Id].TrySetResult(message);
                }
            });
        }

        public void Dispose()
        {
            // Cancels our receiver task
            _receiveCancellationToken.Cancel();
        }

        protected Task<CoapMessage> GetResponseAsync(int messageId)
        {
            TaskCompletionSource<CoapMessage> responseTask = null;
            if (!_messageReponses.TryGetValue(messageId, out responseTask))
                throw new ArgumentOutOfRangeException("messageId does not exist");

            // ToDo: Wait minimum timout period
            responseTask.Task.Wait();

            // ToDo: if wait timed out, retry sending message with back-off delay
            _messageReponses.TryRemove(messageId, out responseTask);
            CoapMessage _;
            _messageQueue.TryRemove(messageId, out _);

            return responseTask.Task;
        }

        public async Task SendAsync(CoapMessage message)
        {
            if (message.Id == 0)
                message.Id = _messageId++;

            await _transport.SendAsync(new CoapPayload { Payload = message.Serialise(), MessageId = message.Id });
        }

        public async Task<CoapMessage> GetAsync(string uri)
        {
            var message = new CoapMessage
            {
                Id = _messageId++,
                Code = CoapMessageCode.Get,
                Type = CoapMessageType.Confirmable
            };
            message.FromUri(uri);

            _messageReponses.TryAdd(message.Id, new TaskCompletionSource<CoapMessage>());

            await _transport.SendAsync(new CoapPayload { Payload = message.Serialise(), MessageId = message.Id });

            return await GetResponseAsync(message.Id);
        }
    }
}
