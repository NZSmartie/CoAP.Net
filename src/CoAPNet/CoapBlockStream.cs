using CoAPNet.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoAPNet.Options;

namespace CoAPNet
{
    /// <summary>
    /// A Coap Block-Wise Tranfer (RFC 7959) implementation of <see cref="Stream"/>. 
    /// </summary>
    public class CoapBlockStream : Stream
    {
        // Backing field for DefaultBlockSize
        private static int _defaultBlockSize = 1024;

        /// <summary>
        /// Gets or Sets the default blocksize used when initailising a new <see cref="CoapBlockStream"/>.
        /// </summary>
        public static int DefaultBlockSize
        {
            get => _defaultBlockSize;
            set => _defaultBlockSize = BlockBase.SupportedBlockSizes.Any(b => b.Item2 == value)
                ? value
                : throw new ArgumentOutOfRangeException();
        }

        private readonly CoapClient _client;
        private readonly ICoapEndpoint _endpoint;

        private readonly ByteQueue _reader = new ByteQueue();
        private readonly Task _readerTask;
        private readonly AsyncAutoResetEvent _readerEvent = new AsyncAutoResetEvent(false);


        private readonly ByteQueue _writer = new ByteQueue();
        private readonly Task _writerTask;
        private readonly AsyncAutoResetEvent _writerEvent = new AsyncAutoResetEvent(false);
        private int _writeBlockNumber;

        private Exception _caughtException;

        private readonly AsyncAutoResetEvent _flushFinishedEvent = new AsyncAutoResetEvent(false);

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly CoapMessage _baseMessage;

        private bool _endOfStream;

        private int _blockSize = DefaultBlockSize;
        private int _readBlockNumber;

        /// <summary>
        /// Gets or sets the maximum amount of time spent writing to <see cref="CoapClient"/> during <see cref="Dispose(bool)"/>
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(-1);

        /// <summary>
        /// Gets or Sets the Blocksize used for transfering data.
        /// </summary>
        /// <remarks>
        /// This can only be set with a decreased value to prevent unexpected behavior.
        /// </remarks>
        public int BlockSize
        {
            get => _blockSize;
            set
            {
                if (value > _blockSize)
                    throw new ArgumentOutOfRangeException($"Can not increase blocksize from {_blockSize} to {value}");

                if (BlockBase.SupportedBlockSizes.All(b => b.Item2 != value))
                    throw new ArgumentOutOfRangeException($"Unsupported blocksize {value}. Expecting block sizes in ({string.Join(", ", Options.BlockBase.SupportedBlockSizes.Select(b => b.Item2))})");

                _blockSize = value;
            }
        }

        /// <inheritdoc/>
        public override bool CanRead => !_endOfStream && (_readerTask?.IsCompleted ?? false);

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => !_endOfStream && (_writerTask?.IsCompleted ?? false);

        /// <summary>
        /// Create a new <see cref="CoapBlockStream"/> using <paramref name="client"/> to read and write blocks of data. <paramref name="baseMessage"/> is required to base blocked messages off of.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="baseMessage"></param>
        /// <param name="endpoint"></param>
        public CoapBlockStream(CoapClient client, CoapMessage baseMessage, ICoapEndpoint endpoint = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _endpoint = endpoint;

            _baseMessage = baseMessage?.Clone()
                           ?? throw new ArgumentNullException(nameof(baseMessage));

            if (!_baseMessage.Code.IsRequest())
                throw new InvalidOperationException($"Can not create a {nameof(CoapBlockStream)} with a {nameof(baseMessage)}.{nameof(baseMessage.Type)} of {baseMessage.Type}");

            _writerTask = WriteBlocksAsync();

            
        }

        /// <summary>
        /// Create a new <see cref="CoapBlockStream"/> using <paramref name="client"/> to read and write blocks of data. <paramref name="baseMessage"/> is required to base blocked messages off of.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="baseMessage"></param>
        /// <param name="endpoint"></param>
        public CoapBlockStream(CoapClient client, CoapMessage baseMessage, CoapMessage requestMessage, ICoapEndpoint endpoint = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _endpoint = endpoint;

            _baseMessage = requestMessage?.Clone()
                           ?? throw new ArgumentNullException(nameof(requestMessage));

            var payload = baseMessage.Payload;

            var block2 = baseMessage.Options.Get<Block2>();
            _blockSize = block2.BlockSize;
            _readBlockNumber = block2.BlockNumber;
            _endOfStream = !block2.IsMoreFollowing;

            _reader.Enqueue(payload, 0, payload.Length);

            _readBlockNumber += payload.Length / _blockSize;

            _readerTask = ReadBlocksAsync();
        }

        private async Task ReadBlocksAsync()
        {
            var cancellationToken = _cancellationTokenSource.Token;
            //var meessageToken = new Random().Next();
            
            try
            {
                while (!_endOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var message = _baseMessage.Clone();
                    message.Id = 0;

                    message.Options.Add(new Block2(_readBlockNumber, _blockSize));

                    var messageId = await _client.SendAsync(message, _endpoint, cancellationToken);

                    var response = await _client.GetResponseAsync(messageId, cancellationToken);

                    if (!response.Code.IsSuccess())
                        throw new CoapBlockException("Error occured while reading blocks from remote endpoint",
                            CoapException.FromCoapMessage(response), response.Code);

                    var block2 = response.Options.Get<Block2>();

                    if (block2.BlockNumber != _readBlockNumber)
                        throw new CoapBlockException("Received incorrect block number from remote host");
                    
                    _readBlockNumber++;

                    _reader.Enqueue(response.Payload, 0, response.Payload.Length);
                    _readerEvent.Set();
                    _endOfStream = !response.Options.Get<Block2>().IsMoreFollowing;
                }
            }
            catch (Exception ex)
            {
                // Hold onto the exception to throw it from a synchronous call.
                _caughtException = ex;
            }
            finally
            {
                _endOfStream = true;
                _readerEvent.Set();
            }
        }

        private async Task WriteBlocksAsync()
        {
            var token = _cancellationTokenSource.Token;
            try
            {
                while (!token.IsCancellationRequested && !_endOfStream)
                {
                    await _writerEvent.WaitAsync(token);

                    while (_writer.Length > BlockSize || (_endOfStream && _writer.Length > 0))
                    {
                        var message = _baseMessage.Clone();

                        // Reset the message Id so it's set by CoapClient
                        message.Id = 0;

                        message.Options.Add(new Options.Block1(_writeBlockNumber, _blockSize, _writer.Length > _blockSize));

                        message.Payload = new byte[_writer.Length < _blockSize ? _writer.Length : _blockSize];
                        _writer.Peek(message.Payload, 0, _blockSize);

                        var messageId = await _client.SendAsync(message, token);

                        var result = await _client.GetResponseAsync(messageId, token);

                        if (result.Code.IsSuccess())
                        {
                            _writer.AdvanceQueue(message.Payload.Length);
                            _writeBlockNumber++;

                            var block = result.Options.Get<Options.Block1>();
                            var blockDelta = block.BlockSize - _blockSize;

                            // Only update the size if it's smaller
                            if (blockDelta < 0)
                            {
                                _blockSize += blockDelta;
                                _writeBlockNumber -= blockDelta / _blockSize;
                            }
                            else if (blockDelta > 0)
                                throw new CoapBlockException($"Remote endpoint requested to increase blocksize from {_blockSize} to {_blockSize + blockDelta}");

                        }
                        else if (result.Code.IsClientError() || result.Code.IsServerError())
                        {
                            if (_writeBlockNumber == 0 && result.Code == CoapMessageCode.RequestEntityTooLarge && _blockSize > 16)
                            {
                                // Try again and attempt at sending a smaller block size.
                                _writeBlockNumber = 0;
                                _blockSize /= 2;

                                continue;
                            }

                            throw new CoapBlockException($"Failed to send block ({_writeBlockNumber}) to remote endpoint", CoapException.FromCoapMessage(result), result.Code);
                        }
                        
                    }

                    // flag the mot recent flush has been performed
                    if(_writer.Length <= BlockSize)
                        _flushFinishedEvent.Set();
                }
            }
            catch (Exception ex)
            {
                // Hold onto the exception to throw it from a synchronous call.
                _caughtException = ex;
            }
            finally
            {
                _endOfStream = true;
                _flushFinishedEvent.Set();
            }
        }

        /// <summary>
        /// Attempt to flush any blocks to <see cref="CoapClient"/> that have been queued up.
        /// </summary>
        /// <inheritdoc/>
        public override void Flush()
        {
            if (_caughtException == null && !_writerTask.IsCompleted)
            {
                _writerEvent.Set();
                _flushFinishedEvent.WaitAsync(CancellationToken.None).Wait();
            }

            ThrowCaughtException();
        }

        /// <summary>
        /// Attempt to flush any blocks to <see cref="CoapClient"/> that have been queued up.
        /// </summary>
        /// <inheritdoc/>
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            _writerEvent.Set();

            await _flushFinishedEvent.WaitAsync(cancellationToken);

            ThrowCaughtException();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = 0;
            while (read < count && !cancellationToken.IsCancellationRequested)
            {
                await _readerEvent.WaitAsync(cancellationToken);

                read += _reader.Dequeue(buffer, offset + read, count - read);
            }

            return read;
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            // TODO: Block here while there are full blocks that can be written. Leaving only incomplete blocks to be written during Close or Dispose
            _writer.Enqueue(buffer, offset, count);
            _writerEvent.Set();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThrowCaughtException();

                _endOfStream = true;

                if(_writerTask != null && !_writerTask.IsCompleted)
                {
                    // Write any/all data to the output
                    if (_writer.Length > 0)
                        _writerEvent.Set();

                    _cancellationTokenSource.CancelAfter(Timeout);

                    try
                    {
                        _writerTask.Wait();
                    }
                    catch (AggregateException ex)
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                }
            }

            base.Dispose(disposing);
        }

        private void ThrowCaughtException()
        {
            if (_caughtException == null)
                return;

            var exception = _caughtException;
            _caughtException = null;
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        #region NotSupppoorted

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        #endregion
    }
}
