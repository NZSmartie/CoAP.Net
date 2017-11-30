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
    /// <summary>
    /// Represents CoAP specific errors that occur during calls inside of <see cref="CoapClient"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CoapClientException : CoapException
    {
        public CoapClientException() : base() { }

        public CoapClientException(string message) : base(message) { }

        public CoapClientException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Client interface for interacting with CoAP endpoints with the provided <see cref="ICoapEndpoint"/>
    /// </summary>
    public class CoapClient : IDisposable
    {
        /// <summary>
        /// The endpoint which this client is bound to, and performs Send/Receive through.
        /// </summary>
        public ICoapEndpoint Endpoint { get; private set; }

        private int _messageId;

        private Queue<Tuple<DateTime, ICoapEndpoint, int>> _recentMessageIds = new Queue<Tuple<DateTime, ICoapEndpoint, int>>();

        /// <summary>
        /// Sets or gets the <see cref="TimeSpan"/> to retain CoAP messageIDs per endpoint to compare repeated messages against.
        /// </summary>
        /// <remarks>Default is <c>5 minutes</c></remarks>
        public TimeSpan IgnoreRepeatesFor { get; set; } = TimeSpan.FromMinutes(5);

        // I'm not particularly fond of the following _messageQueue and _messageResponses... Feels more like a hack. but it works? NEEDS MORE TESTING!!!
        private readonly ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>> _messageResponses 
            = new ConcurrentDictionary<int, TaskCompletionSource<CoapMessage>>();

        /// <summary>
        /// Maximum number of attempts at performing <see cref="ICoapEndpoint.SendAsync(CoapPacket)"/> before throwing <see cref="CoapClientException"/>
        /// </summary>
        /// <remarks>Default value is set to <see cref="Coap.MaxRestransmitAttempts"/></remarks>
        public int MaxRetransmitAttempts { get; set; } = Coap.MaxRestransmitAttempts;

        /// <summary>
        /// Interval to wait after sending a CoAP packet.
        /// </summary>
        /// <remarks>Default value is set to <see cref="Coap.RetransmitTimeout"/></remarks>
        public TimeSpan RetransmitTimeout { get; set; } = Coap.RetransmitTimeout;

        /// <summary>
        /// Initiates a mew client and assigns a random message Id to be used in new CoAP requests.
        /// </summary>
        /// <param name="endpoint">The local CoAP endpoint to bind to and perform Send/Receive operations on.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="endpoint"/> is <c>null</c></exception>
        public CoapClient(ICoapEndpoint endpoint)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            _messageId = new Random().Next() & 0xFFFF;
        }

        private readonly ConcurrentQueue<Task<CoapReceiveResult>> _receiveQueue = new ConcurrentQueue<Task<CoapReceiveResult>>();
        private Task _receiveTask = Task.CompletedTask;

        private readonly AsyncAutoResetEvent _receiveEvent = new AsyncAutoResetEvent(false);

        public async Task<CoapReceiveResult> ReceiveAsync()
            => await ReceiveAsync(RetransmitTimeout.Milliseconds * MaxRetransmitAttempts);

        public async Task<CoapReceiveResult> ReceiveAsync(int milliseconds)
            => await ReceiveAsync(new CancellationTokenSource(milliseconds).Token);

        public async Task<CoapReceiveResult> ReceiveAsync(TimeSpan timeout)
            => await ReceiveAsync(new CancellationTokenSource(timeout).Token);

        /// <summary>
        /// Checks if a <see cref="CoapReceiveResult"/> has been received and returns it. Otherwise waits until a new result is received unless cancelled by the <paramref name="token"/>
        /// </summary>
        /// <param name="token">Token to cancel the blocking Receive operation</param>
        /// <returns>Valid result if a result is received, <c>null</c> if canceled.</returns>
        public async Task<CoapReceiveResult> ReceiveAsync(CancellationToken token)
        {
            Task<CoapReceiveResult> resultTask = null;
            lock (_receiveQueue)
            {
                while (!_receiveQueue.IsEmpty)
                {
                    if (_receiveQueue.TryDequeue(out resultTask))
                        break;
                }
            }
            if (resultTask != null)
                return resultTask.IsCanceled
                    ? null
                    : await resultTask;
            
            if (Endpoint == null)
                throw new InvalidOperationException($"{nameof(CoapClient)} is in an invalid state");

            StartReceiveAsyncInternal();

            do
            {
                await _receiveEvent.WaitAsync(token);

                if (token.IsCancellationRequested)
                    return null;

                if (_receiveQueue.TryDequeue(out resultTask))
                    break;
            } while (true);
            

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

                    var message = new CoapMessage { IsMulticast = Endpoint.IsMulticast };
                    try
                    {
                        message.FromBytes(payload.Payload);

                        // Ignore non-empty reset messages
                        if (message.Type == CoapMessageType.Reset && message.Code != CoapMessageCode.None)
                            continue;

                        if (IsRepeated(payload.Endpoint, message.Id))
                            continue;

                        _recentMessageIds.Enqueue(Tuple.Create(DateTime.Now, payload.Endpoint, message.Id));
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

                    _receiveQueue.Enqueue(Task.FromResult(new CoapReceiveResult(payload.Endpoint, message)));
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

                // Gona cheat and enque that exception so it gets thrown as if this detached-infinite-loop never existed...
                _receiveQueue.Enqueue(Task.FromException<CoapReceiveResult>(ex));

                _receiveEvent.Set();

            }
        }

        private bool IsRepeated(ICoapEndpoint endpoint, int messageId)
        {
            var clearBefore = DateTime.Now.Subtract(IgnoreRepeatesFor);

            // Clear out expired messageIds
            while (_recentMessageIds.Count > 0)
            {
                var p = _recentMessageIds.Peek();
                if (p.Item1 < clearBefore)
                    _recentMessageIds.Dequeue();
                else
                    break;
            }

            if (_recentMessageIds.Any(r => r.Item3 == messageId && r.Item2 == endpoint))
                return true;

            return false;
        }

        /// <summary>
        /// Disposed the underlying <see cref="Endpoint"/> and waits for any pending operations to finish.
        /// </summary>
        /// <exception cref="CoapClientException">If the internal Receive operation is blocked from exiting.</exception>
        public void Dispose()
        {
            var endpoint = Endpoint;
            Endpoint = null;

            endpoint?.Dispose();

            if (!_receiveTask.IsCompleted && !_receiveTask.IsCanceled && !_receiveTask.IsFaulted && !_receiveTask.Wait(5000))
                throw new CoapClientException($"Took too long to dispose of the enclosed {nameof(Endpoint)}");
        }

        /// <summary>
        /// Checks if a <see cref="CoapReceiveResult"/> has been received with a coresponding <paramref name="messageId"/> and returns it. Otherwise waits until a new result is received unless cancelled by the <paramref name="token"/> or the <see cref="MaxRetransmitAttempts"/> is reached.
        /// </summary>
        /// <param name="messageId">Waits for a result with a coresponding message Id.</param>
        /// <param name="token">Token to cancel the blocking Receive operation</param>
        /// <returns>Valid result if a result is received, <c>null</c> if canceled.</returns>
        /// <exception cref="CoapClientException">If the timeout period * maximum retransmission attempts was reached.</exception>
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

        /// <summary>
        /// <see cref="SendAsync(CoapMessage, ICoapEndpoint, CancellationToken)"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual async Task<int> SendAsync(CoapMessage message) 
            => await SendAsync(message, null, CancellationToken.None);

        /// <summary>
        /// <see cref="SendAsync(CoapMessage, ICoapEndpoint, CancellationToken)"/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async Task<int> SendAsync(CoapMessage message, CancellationToken token) 
            => await SendAsync(message, null, token);

        /// <summary>
        /// <see cref="SendAsync(CoapMessage, ICoapEndpoint, CancellationToken)"/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public virtual async Task<int> SendAsync(CoapMessage message, ICoapEndpoint endpoint) 
            => await SendAsync(message, endpoint, CancellationToken.None);

        private int GetNextMessageId() 
            => Interlocked.Increment(ref _messageId) & ushort.MaxValue;

        /// <summary>
        /// Assigns an incremented message id (if none is already set) and performs a blocking Send operation. 
        /// <para>If the mssage is <see cref="CoapMessageType.Confirmable"/>, the client will wait for a response with a coresponding message Id for the <see cref="RetransmitTimeout"/>* * <see cref="MaxRetransmitAttempts"/></para>
        /// </summary>
        /// <param name="message">The CoAP message to send. It's <see cref="CoapMessage.Id"/> may be set if it is unassigned.</param>
        /// <param name="endpoint">The remote endpoint to send the message to.
        /// <para>The endpoint must implement the same underlying transport to succeed.</para>
        /// </param>
        /// <param name="token">A token used to cancel the blocking Send operation or retransmission attempts.</param>
        /// <returns>The message Id</returns>
        /// <exception cref="CoapClientException">If the timeout period * maximum retransmission attempts was reached.</exception>
        public virtual async Task<int> SendAsync(CoapMessage message, ICoapEndpoint endpoint, CancellationToken token)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if(Endpoint==null)
                throw new InvalidOperationException($"{nameof(CoapClient)} is in an invalid state");

            if (message.Id == 0)
                message.Id = GetNextMessageId();

            if (message.IsMulticast && message.Type != CoapMessageType.NonConfirmable)
                throw new CoapClientException("Can not send confirmable (CON) CoAP message to a multicast endpoint");

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
            if (remoteEndpoint == null)
                remoteEndpoint = new CoapEndpoint
                {
                    BaseUri = new UriBuilder(message.GetUri()) { Path = "/", Fragment = "", Query = "" }.Uri,
                    IsMulticast = message.IsMulticast,
                };
            else if (message.IsMulticast && !remoteEndpoint.IsMulticast)
                throw new CoapClientException("Can not send CoAP multicast message to a non-multicast endpoint");

            await Task.Run(async () => await (Endpoint?.SendAsync(new CoapPacket
            {
                Payload = message.ToBytes(),
                Endpoint = remoteEndpoint
            }) ?? Task.CompletedTask), token).ConfigureAwait(false);
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
            message.SetUri(uri);

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
