using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoAPNet;

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
        public ICoapEndpoint Endpoint { get; }

        private int _messageId;

        // I'm not particularly fond of the following _messageQueue and _messageResponses... Feels more like a hack. but it works? NEEDS MORE TESTING!!!
        private readonly ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>> _messageReponses 
            = new ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>>();

        public CoapClient(ICoapEndpoint endpoint)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            _messageId = new Random().Next() & 0xFFFF;
        }

        public async Task<CoapReceiveResult> ReceiveAsync(CancellationToken token)
        {
            var payload = await Endpoint.ReceiveAsync(token);
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
                    }, payload.Endpoint, token);
                }
                throw;
            }

            if (_messageReponses.ContainsKey(message.Id))
                _messageReponses[message.Id].TrySetResult(message);

            return new CoapReceiveResult(payload.Endpoint, message);
        }

        public void Dispose()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            (Endpoint as IDisposable)?.Dispose();
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

        public virtual async Task<int> SendAsync(CoapMessage message, ICoapEndpoint endpoint, CancellationToken token)
        {
            unchecked
            {
                if (message.Id == 0)
                    message.Id = Interlocked.Increment(ref _messageId) & ushort.MaxValue;
            }

            if(message.Type == CoapMessageType.Confirmable)
                _messageReponses.TryAdd(message.Id, new TaskCompletionSource<CoapMessage>());

            await Endpoint
                .SendAsync(new CoapPacket
                {
                    Payload = message.Serialise(),
                    MessageId = message.Id,
                    Endpoint = endpoint
                }, token)
                .ConfigureAwait(false);
            
            return message.Id;
        }

        public virtual async Task<int> GetAsync(string uri, ICoapEndpoint endpoint = null)
        {
            var message = new CoapMessage
            {
                Code = CoapMessageCode.Get,
                Type = CoapMessageType.Confirmable
            };
            message.FromUri(uri);

            _messageReponses.TryAdd(message.Id, new TaskCompletionSource<CoapMessage>());

            return await SendAsync(message, endpoint).ConfigureAwait(false);
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
