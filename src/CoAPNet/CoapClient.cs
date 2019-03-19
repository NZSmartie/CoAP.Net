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
using System.Reflection;
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
        /// <inheritdoc/>
        public CoapClientException() 
            : base()
        { }

        /// <inheritdoc/>
        public CoapClientException(string message) 
            : base(message)
        { }

        /// <inheritdoc/>
        public CoapClientException(string message, Exception innerException) 
            : base(message, innerException)
        { }

        /// <inheritdoc/>
        public CoapClientException(string message, CoapMessageCode responseCode) 
            : base(message, responseCode)
        { }

        /// <inheritdoc/>
        public CoapClientException(string message, Exception innerException, CoapMessageCode responseCode) 
            : base(message, innerException, responseCode)
        { }
    }

    /// <summary>
    /// Client interface for interacting with CoAP endpoints with the provided <see cref="ICoapEndpoint"/>
    /// </summary>
    public partial class CoapClient : IDisposable
    {
        /// <summary>
        /// The endpoint which this client is bound to, and performs Send/Receive through.
        /// </summary>
        public ICoapEndpoint Endpoint { get; private set; }

        private int _messageId;

        private readonly Queue<Tuple<DateTime, ICoapEndpoint, CoapMessage>> _recentMessages = new Queue<Tuple<DateTime, ICoapEndpoint, CoapMessage>>();

        // TODO: Test this default value. would only a few seconds be enough?
        /// <summary>
        /// Sets or gets the <see cref="TimeSpan"/> to retain CoAP messageIDs per endpoint to compare repeated messages against.
        /// </summary>
        /// <remarks>Default is <c>5 minutes</c></remarks>
        public TimeSpan MessageCacheTimeSpan { get; set; } = TimeSpan.FromMinutes(1);

        // I'm not particularly fond of the following _messageQueue and _messageResponses... Feels more like a hack. but it works? NEEDS MORE TESTING!!!
        private readonly ConcurrentDictionary<CoapMessageIdentifier, TaskCompletionSource<CoapMessage>> _messageResponses 
            = new ConcurrentDictionary<CoapMessageIdentifier, TaskCompletionSource<CoapMessage>>();

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
        private readonly CancellationTokenSource _receiveTaskCTS = new CancellationTokenSource();

        private readonly AsyncAutoResetEvent _receiveEvent = new AsyncAutoResetEvent(false);

        /// <summary>
        /// Checks if a <see cref="CoapReceiveResult"/> has been received and returns it. Otherwise waits until a new result is received unless max retransmission attempts is reached.
        /// </summary>
        /// <returns>Valid result if a result is received, <c>null</c> if canceled.</returns>
        public async Task<CoapReceiveResult> ReceiveAsync()
            => await ReceiveAsync(RetransmitTimeout.Milliseconds * MaxRetransmitAttempts);

        /// <summary>
        /// Checks if a <see cref="CoapReceiveResult"/> has been received and returns it. Otherwise waits until a new result is received unless timed out by <paramref name="milliseconds"/>
        /// </summary>
        /// <returns>Valid result if a result is received, <c>null</c> if timed out.</returns>
        public async Task<CoapReceiveResult> ReceiveAsync(int milliseconds)
            => await ReceiveAsync(new CancellationTokenSource(milliseconds).Token);

        /// <summary>
        /// Checks if a <see cref="CoapReceiveResult"/> has been received and returns it. Otherwise waits until a new result is received unless timed out by <paramref name="timeout"/>
        /// </summary>
        /// <returns>Valid result if a result is received, <c>null</c> if timed out.</returns>
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
                return await resultTask;
            
            if (Endpoint == null)
                throw new CoapEndpointException($"{nameof(CoapClient)} does not have a valid endpoint");

            StartReceiveAsyncInternal();

            do
            {
                token.ThrowIfCancellationRequested();
                    
                await _receiveEvent.WaitAsync(token);

            } while (!_receiveQueue.TryDequeue(out resultTask));
            
            // Task sould already be in the completed,cancelled or error'd state.
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
            var isMulticast = Endpoint?.IsMulticast ?? false;
            var cancellationToken = _receiveTaskCTS.Token;
            try
            {
                while (true)
                {
                    var endpoint = Endpoint;
                    if (endpoint == null)
                        throw new CoapEndpointException("Endpoint is disposed");

                    var payload = await endpoint.ReceiveAsync(cancellationToken);
                    var receivedAt = DateTime.Now;

                    var message = new CoapMessage { IsMulticast = isMulticast };
                    try
                    {
                        message.FromBytes(payload.Payload);

                        // Ignore non-empty reset messages
                        if (message.Type == CoapMessageType.Reset && message.Code != CoapMessageCode.None)
                            continue;

                        // Reject confirmable empty messages
                        if (message.Type == CoapMessageType.Confirmable && message.Code == CoapMessageCode.None)
                        {
                            await SendAsync(new CoapMessage { Id = message.Id, Type = CoapMessageType.Reset }, payload.Endpoint);
                            continue;
                        }

                        // Ignore repeated messages
                        if (IsRepeated(payload.Endpoint, message.Id))
                            continue;

                    }
                    catch (CoapMessageFormatException)
                    {
                        if (message.Type == CoapMessageType.Confirmable
                            && !isMulticast)
                        {
                            await SendAsync(new CoapMessage
                            {
                                Id = message.Id,
                                Type = CoapMessageType.Reset
                            }, payload.Endpoint);
                        }
                        if (message.Type == CoapMessageType.Acknowledgement
                            && Coap.ReservedMessageCodeClasses.Contains(message.Code.Class))
                            continue;

                        throw;
                    }

                    lock (_recentMessages)
                    {
                        var messageId = message.GetIdentifier(payload.Endpoint);

                        if (_messageResponses.ContainsKey(messageId))
                            _messageResponses[messageId].TrySetResult(message);

                        _recentMessages.Enqueue(Tuple.Create(receivedAt, payload.Endpoint, message));
                    }

                    _receiveQueue.Enqueue(Task.FromResult(new CoapReceiveResult(payload.Endpoint, message)));
                    _receiveEvent.Set();
                }
            }
            catch (Exception ex)
            {
                if (ex is CoapEndpointException)
                {
                    var endpoint = Endpoint;
                    Endpoint = null;
                    
                    endpoint?.Dispose();

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
            var clearBefore = DateTime.Now.Subtract(MessageCacheTimeSpan);

            lock (_recentMessages)
            {
                // Clear out expired messageIds
                while (_recentMessages.Count > 0)
                {
                    if(_recentMessages.Peek().Item1 >= clearBefore)
                        break;
                    _recentMessages.Dequeue();
                }

                if (_recentMessages.Any(r => r.Item3.Id == messageId && r.Item2 == endpoint))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Disposed the underlying <see cref="Endpoint"/> and waits for any pending operations to finish.
        /// </summary>
        /// <exception cref="CoapClientException">If the internal Receive operation is blocked from exiting.</exception>
        public void Dispose()
        {
            var endpoint = Endpoint;
            try
            {
                Endpoint = null;

                _receiveTaskCTS.Cancel();

                if (!_receiveTask.Wait(5000) && !_receiveTask.IsCompleted && !_receiveTask.IsCanceled && !_receiveTask.IsFaulted)
                    throw new CoapClientException($"Took too long to dispose of the enclosed {nameof(Endpoint)}");
            }
            finally
            {
                endpoint?.Dispose();
            }
        }

        /// <summary>
        /// Checks if a <see cref="CoapReceiveResult"/> has been received for the coresponding <paramref name="request"/> and returns it. 
        /// Otherwise waits until a new result is received unless cancelled by the <paramref name="token"/> or the <see cref="MaxRetransmitAttempts"/> is reached.
        /// </summary>
        /// <param name="request">Waits for a result with a coresponding request message.</param>
        /// <param name="token">Token to cancel the blocking Receive operation</param>
        /// <returns>Valid result if a result is received, <c>null</c> if canceled.</returns>
        /// <exception cref="CoapClientException">If the timeout period * maximum retransmission attempts was reached.</exception>
        public Task<CoapMessage> GetResponseAsync(CoapMessage request, ICoapEndpoint endpoint = null, bool isRequest = false, CancellationToken token = default(CancellationToken), bool dequeue = true)
            => GetResponseAsync(request.GetIdentifier(endpoint, isRequest), token, dequeue);


        /// <summary>
        /// Checks if a <see cref="CoapReceiveResult"/> has been received with a coresponding <paramref name="messageId"/> and returns it. Otherwise waits until a new result is received unless cancelled by the <paramref name="token"/> or the <see cref="MaxRetransmitAttempts"/> is reached.
        /// </summary>
        /// <param name="messageId">Waits for a result with a coresponding message Id.</param>
        /// <param name="token">Token to cancel the blocking Receive operation</param>
        /// <returns>Valid result if a result is received, <c>null</c> if canceled.</returns>
        /// <exception cref="CoapClientException">If the timeout period * maximum retransmission attempts was reached.</exception>
        [Obsolete("In favor of GetResponseAsync(CoapMessage request, ...)")]
        public Task<CoapMessage> GetResponseAsync(int messageId, CancellationToken token = default(CancellationToken), bool dequeue = true)
        {
            // Assume message was Confirmable
            return GetResponseAsync(new CoapMessageIdentifier(messageId, CoapMessageType.Confirmable), token, dequeue);
        }

        // TODO: Ignore Acks, we're actually interested in a response.
        public async Task<CoapMessage> GetResponseAsync(CoapMessageIdentifier messageId, CancellationToken token = default(CancellationToken), bool dequeue = true)
        {
            TaskCompletionSource<CoapMessage> responseTask = null;

            if (!_messageResponses.TryGetValue(messageId, out responseTask))
                throw new CoapClientException($"The current message id ({messageId}) is not pending a due response");

            if (responseTask.Task.IsCompleted)
                lock(_recentMessages)
                    return _recentMessages.FirstOrDefault(m => m.Item3.GetIdentifier() == messageId)?.Item3
                        ?? throw new CoapClientException($"No recent message found for {messageId}. This may happen when {nameof(MessageCacheTimeSpan)} is too short");

            if (Endpoint == null)
                throw new CoapEndpointException($"{nameof(CoapClient)} has an invalid {nameof(Endpoint)}");

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

            if (dequeue)
                _messageResponses.TryRemove(messageId, out _);


            return responseTask.Task.Result;
        }

        /// <summary>
        /// <see cref="SendAsync(CoapMessage, ICoapEndpoint, CancellationToken)"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual async Task<CoapMessageIdentifier> SendAsync(CoapMessage message) 
            => await SendAsync(message, null, CancellationToken.None);

        /// <summary>
        /// <see cref="SendAsync(CoapMessage, ICoapEndpoint, CancellationToken)"/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async Task<CoapMessageIdentifier> SendAsync(CoapMessage message, CancellationToken token) 
            => await SendAsync(message, null, token);

        /// <summary>
        /// <see cref="SendAsync(CoapMessage, ICoapEndpoint, CancellationToken)"/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public virtual async Task<CoapMessageIdentifier> SendAsync(CoapMessage message, ICoapEndpoint endpoint) 
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
        public virtual async Task<CoapMessageIdentifier> SendAsync(CoapMessage message, ICoapEndpoint endpoint, CancellationToken token)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (Endpoint == null)
                throw new CoapEndpointException($"{nameof(CoapClient)} has an invalid {nameof(Endpoint)}");

            if (message.Id == 0)
                message.Id = GetNextMessageId();

            if (message.IsMulticast && message.Type != CoapMessageType.NonConfirmable)
                throw new CoapClientException("Can not send confirmable (CON) CoAP message to a multicast endpoint");

            var messageId = message.GetIdentifier(endpoint, message.Type == CoapMessageType.Confirmable || message.Type == CoapMessageType.NonConfirmable);

            _messageResponses.TryAdd(messageId, new TaskCompletionSource<CoapMessage>(TaskCreationOptions.RunContinuationsAsynchronously));

            if (message.Type != CoapMessageType.Confirmable)
            {
                await SendAsyncInternal(message, endpoint, token).ConfigureAwait(false);
                return messageId;
            }

            if (!_messageResponses.TryGetValue(messageId, out var responseTaskSource))
                throw new CoapClientException("Race condition? This shouldn't happen. Congratuations!");

            for (var attempt = 1; attempt <= MaxRetransmitAttempts; attempt++)
            {
                StartReceiveAsyncInternal();

                await SendAsyncInternal(message, endpoint, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                var timeout = TimeSpan.FromMilliseconds(RetransmitTimeout.TotalMilliseconds * attempt);

                await Task.WhenAny(responseTaskSource.Task, Task.Delay(timeout, token)).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();


                if (responseTaskSource.Task.IsCompleted)
                    return messageId;
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

            Task task;
            lock (this)
            {
                task = (Endpoint == null)
                    ? Task.CompletedTask
                    : Endpoint.SendAsync(new CoapPacket
                    {
                        Payload = message.ToBytes(),
                        Endpoint = remoteEndpoint
                    }, token);
            }

            await task.ConfigureAwait(false);
        }

        internal void SetNextMessageId(int value)
        {
            Interlocked.Exchange(ref _messageId, value - 1);
        }
        
#region Request Operations

        /// <summary>
        /// Performs a async <see cref="CoapMessageCode.Get"/> request to the <paramref name="uri"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async Task<CoapMessageIdentifier> GetAsync(string uri, CancellationToken token = default(CancellationToken))
        {
            return await GetAsync(uri, null, token);
        }

        /// <summary>
        /// Performs a async <see cref="CoapMessageCode.Get"/> request with a supplied <paramref name="uri"/> to the <paramref name="endpoint"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="endpoint"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async Task<CoapMessageIdentifier> GetAsync(string uri, ICoapEndpoint endpoint, CancellationToken token = default(CancellationToken))
        {
            var message = new CoapMessage
            {
                Code = CoapMessageCode.Get,
                Type = CoapMessageType.Confirmable
            };
            message.SetUri(uri);

            return await SendAsync(message, endpoint, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a async <see cref="CoapMessageCode.Put"/> request to the <paramref name="uri"/> with the supplied <paramref name="message"/> payload.
        /// Optionally, a <paramref name="endpoint"/> may be specified for sending the request to.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="message"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public virtual Task<int> PutAsync(string uri, CoapMessage message, ICoapEndpoint endpoint = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs a async <see cref="CoapMessageCode.Post"/> request to the <paramref name="uri"/> with the supplied <paramref name="message"/> payload.
        /// Optionally, a <paramref name="endpoint"/> may be specified for sending the request to.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="message"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public virtual Task<int> PostAsync(string uri, CoapMessage message, ICoapEndpoint endpoint = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs a async <see cref="CoapMessageCode.Delete"/> to the supplied <paramref name="uri"/>.
        /// Optionally, a <paramref name="endpoint"/> may be specified for sending the request to.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
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

    /// <summary>
    /// A object to hold a <see cref="CoapMessage"/> and <see cref="ICoapEndpoint"/> result.
    /// </summary>
    public class CoapReceiveResult
    {
        /// <summary>
        /// Initialise a new <see cref="CoapReceiveResult"/>.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="message"></param>
        public CoapReceiveResult(ICoapEndpoint endpoint, CoapMessage message)
        {
            Endpoint = endpoint;

            Message = message;
        }

        /// <summary>
        /// Gets which <see cref="ICoapEndpoint"/> the <see cref="Message"/> was received from.
        /// </summary>
        public ICoapEndpoint Endpoint { get; }

        /// <summary>
        /// The <see cref="CoapMessage"/> that was received from <see cref="Endpoint"/>.
        /// </summary>
        public CoapMessage Message { get; }
    }
}
