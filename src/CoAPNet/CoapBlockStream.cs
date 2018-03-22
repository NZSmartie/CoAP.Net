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
    public abstract class CoapBlockStream : Stream
    {
        // Backing field for DefaultBlockSize
        private static int _defaultBlockSize = 1024;

        /// <summary>
        /// Gets or Sets the default blocksize used when initailising a new <see cref="CoapBlockStreamWriter"/>.
        /// </summary>
        public static int DefaultBlockSize
        {
            get => _defaultBlockSize;
            set => _defaultBlockSize = BlockBase.InternalSupportedBlockSizes.Any(b => b.Item2 == value)
                ? value
                : throw new ArgumentOutOfRangeException();
        }

        protected int BlockSizeInternal = DefaultBlockSize;

        protected readonly ICoapEndpoint Endpoint;

        protected Exception CaughtException;

        protected readonly AsyncAutoResetEvent FlushFinishedEvent = new AsyncAutoResetEvent(false);

        protected readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        public readonly CoapBlockWiseContext Context;

        protected bool EndOfStream;

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
            get => BlockSizeInternal;
            set
            {
                if (value > BlockSizeInternal)
                    throw new ArgumentOutOfRangeException($"Can not increase blocksize from {BlockSizeInternal} to {value}");

                if (BlockBase.InternalSupportedBlockSizes.All(b => b.Item2 != value))
                    throw new ArgumentOutOfRangeException($"Unsupported blocksize {value}. Expecting block sizes in ({string.Join(", ", Options.BlockBase.InternalSupportedBlockSizes.Select(b => b.Item2))})");

                BlockSizeInternal = value;
            }
        }

        protected CoapBlockStream(CoapBlockWiseContext context, ICoapEndpoint endpoint = null)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));

            Endpoint = endpoint;
        }

        /// <summary>
        /// Gets the last <see cref="CoapMessageIdentifier"/> for the Block-Wise message stream. This may be used to retreive the the response from from the <see cref="CoapClient"/> by invokeing <see cref="CoapClient.GetResponseAsync(CoapMessageIdentifier, CancellationToken, bool)"/>
        /// </summary>
        /// <param name="ct">A cancelation token to cancel the async operation.</param>
        /// <returns>The very last <see cref="CoapMessageIdentifier"/> or <code>default</code> if the block-wise opration completed prematurely.</returns>
        //public Task<CoapMessageIdentifier> GetFinalMessageIdAsync(CancellationToken ct = default)
        //    => Task.Run(async () => await CoapMessageIdTask.Task, ct);

        protected void ThrowExceptionIfCaught()
        {
            if (CaughtException == null)
                return;

            var exception = CaughtException;
            CaughtException = null;
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    /// <summary>
    /// A Coap Block-Wise Tranfer (RFC 7959) implementation of <see cref="Stream"/>. 
    /// </summary>
    public class CoapBlockStreamWriter : CoapBlockStream
    {
        private readonly ByteQueue _writer = new ByteQueue();
        private readonly Task _writerTask;
        private readonly AsyncAutoResetEvent _writerEvent = new AsyncAutoResetEvent(false);
        private int _writeBlockNumber;

        /// <inheritdoc/>
        public override bool CanRead => false;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => !EndOfStream && (_writerTask?.IsCompleted ?? false);

        /// <summary>
        /// Create a new <see cref="CoapBlockStreamWriter"/> using <paramref name="client"/> to read and write blocks of data. <paramref name="baseMessage"/> is required to base blocked messages off of.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="baseMessage"></param>
        /// <param name="endpoint"></param>
        public CoapBlockStreamWriter(CoapClient client, CoapMessage baseMessage, ICoapEndpoint endpoint = null)
            : this(baseMessage.CreateBlockWiseContext(client), endpoint)
        { }

        public CoapBlockStreamWriter(CoapBlockWiseContext context, ICoapEndpoint endpoint = null)
            : base(context, endpoint)
        {
            if (!Context.Request.Code.IsRequest())
                throw new InvalidOperationException($"Can not create a {nameof(CoapBlockStreamWriter)} with a {nameof(context)}.{nameof(context.Request)}.{nameof(CoapMessage.Type)} of {context.Request.Type}");

            _writerTask = WriteBlocksAsync();
        }

        private async Task WriteBlocksAsync()
        {
            var token = CancellationTokenSource.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await _writerEvent.WaitAsync(token);

                    while (_writer.Length > BlockSize || (EndOfStream && _writer.Length > 0))
                    {
                        var message = Context.Request.Clone();

                        // Reset the message Id so it's set by CoapClient
                        message.Id = 0;

                        message.Options.Add(new Options.Block1(_writeBlockNumber, BlockSizeInternal, _writer.Length > BlockSizeInternal));

                        message.Payload = new byte[_writer.Length < BlockSizeInternal ? _writer.Length : BlockSizeInternal];
                        _writer.Peek(message.Payload, 0, BlockSizeInternal);

                        Context.MessageId = await Context.Client.SendAsync(message, token);

                        // Keep the response in the queue in case the Applciation needs it. 
                        var result = await Context.Client.GetResponseAsync(Context.MessageId, token);

                        if (EndOfStream)
                            Context.Response = result;

                        if (result.Code.IsSuccess())
                        {
                            _writer.AdvanceQueue(message.Payload.Length);
                            _writeBlockNumber++;

                            var block = result.Options.Get<Options.Block1>();
                            var blockDelta = block.BlockSize - BlockSizeInternal;

                            // Only update the size if it's smaller
                            if (blockDelta < 0)
                            {
                                BlockSizeInternal += blockDelta;
                                _writeBlockNumber -= blockDelta / BlockSizeInternal;
                            }
                            else if (blockDelta > 0)
                                throw new CoapBlockException($"Remote endpoint requested to increase blocksize from {BlockSizeInternal} to {BlockSizeInternal + blockDelta}");

                        }
                        else if (result.Code.IsClientError() || result.Code.IsServerError())
                        {
                            if (_writeBlockNumber == 0 && result.Code == CoapMessageCode.RequestEntityTooLarge && BlockSizeInternal > 16)
                            {
                                // Try again and attempt at sending a smaller block size.
                                _writeBlockNumber = 0;
                                BlockSizeInternal /= 2;

                                continue;
                            }

                            Context.Response = result;
                            throw new CoapBlockException($"Failed to send block ({_writeBlockNumber}) to remote endpoint", CoapException.FromCoapMessage(result), result.Code);
                        }
                    }

                    // flag the mot recent flush has been performed
                    if(_writer.Length <= BlockSize)
                        FlushFinishedEvent.Set();

                    if (EndOfStream)
                        break;
                }
            }
            catch (Exception ex)
            {
                // Hold onto the exception to throw it from a synchronous call.
                CaughtException = ex;
            }
            finally
            {
                EndOfStream = true;
                FlushFinishedEvent.Set();
            }
        }

        /// <summary>
        /// Attempt to flush any blocks to <see cref="CoapClient"/> that have been queued up.
        /// </summary>
        /// <inheritdoc/>
        public override void Flush()
        {
            if (CaughtException == null && !_writerTask.IsCompleted)
            {
                _writerEvent.Set();
                FlushFinishedEvent.WaitAsync(CancellationToken.None).Wait();
            }

            ThrowExceptionIfCaught();
        }

        /// <summary>
        /// Attempt to flush any blocks to <see cref="CoapClient"/> that have been queued up.
        /// </summary>
        /// <inheritdoc/>
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            _writerEvent.Set();

            await FlushFinishedEvent.WaitAsync(cancellationToken);

            ThrowExceptionIfCaught();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (EndOfStream)
                throw new EndOfStreamException("Stream ended before all bytes were written", CaughtException);

            // Lets artificailly block while the writer task has blocks to write.
            if (_writer.Length > BlockSize)
                await FlushFinishedEvent.WaitAsync(cancellationToken);

            _writer.Enqueue(buffer, offset, count);
            _writerEvent.Set();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThrowExceptionIfCaught();

                EndOfStream = true;

                if(_writerTask != null && !_writerTask.IsCompleted)
                {
                    // Write any/all data to the output
                    if (_writer.Length > 0)
                        _writerEvent.Set();

                    CancellationTokenSource.CancelAfter(Timeout);

                    try
                    {
                        _writerTask.Wait();
                    }
                    catch (AggregateException ex)
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }

                    ThrowExceptionIfCaught();
                }
            }

            base.Dispose(disposing);
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

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        #endregion
    }

    /// <summary>
    /// A Coap Block-Wise Tranfer (RFC 7959) implementation of <see cref="Stream"/>. 
    /// </summary>
    public class CoapBlockStreamReader : CoapBlockStream
    {
        private readonly ByteQueue _reader = new ByteQueue();
        private readonly Task _readerTask;
        private readonly AsyncAutoResetEvent _readerEvent = new AsyncAutoResetEvent(false);

        private int _readBlockNumber;

        /// <inheritdoc/>
        public override bool CanRead => !EndOfStream || _reader.Length > 0;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <summary>
        /// Create a new <see cref="CoapBlockStreamWriter"/> using <paramref name="client"/> to read and write blocks of data. <paramref name="response"/> is required to base blocked messages off of.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="response"></param>
        /// <param name="request"></param>
        /// <param name="endpoint"></param>
        public CoapBlockStreamReader(CoapClient client, CoapMessage respose, CoapMessage request, ICoapEndpoint endpoint = null)
            : this(request.CreateBlockWiseContext(client, respose), endpoint)
        { }

        public CoapBlockStreamReader(CoapBlockWiseContext context, ICoapEndpoint endpoint = null)
            : base(context, endpoint)
        {
            if (context.Response == null)
                throw new ArgumentNullException($"{nameof(context)}.{nameof(context.Response)}");

            var payload = Context.Response.Payload;
            Context.Request.Payload = null;
            Context.Response.Payload = null;

            if (payload != null)
                _reader.Enqueue(payload, 0, payload.Length);

            var block2 = Context.Response.Options.Get<Block2>();
            if(block2 != null)
            {
                _readBlockNumber = block2.BlockNumber;

                BlockSizeInternal = block2.BlockSize;
                EndOfStream = !block2.IsMoreFollowing;

                if (payload != null)
                    _readBlockNumber += payload.Length / BlockSizeInternal;

                _readerTask = ReadBlocksAsync();
            }
            else
            {
                EndOfStream = true;
                _readerTask = Task.CompletedTask;
            }
        }

        private async Task ReadBlocksAsync()
        {
            var cancellationToken = CancellationTokenSource.Token;

            try
            {
                while (!EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var message = Context.Request.Clone();
                    message.Id = 0;

                    // Strip out any block options
                    message.Options.RemoveAll(o => o is Block1 || o is Block2);

                    message.Options.Add(new Block2(_readBlockNumber, BlockSizeInternal));

                    Context.MessageId = await Context.Client.SendAsync(message, Endpoint, cancellationToken);

                    var response = await Context.Client.GetResponseAsync(Context.MessageId, cancellationToken);

                    if (!response.Code.IsSuccess())
                        throw new CoapBlockException("Error occured while reading blocks from remote endpoint",
                            CoapException.FromCoapMessage(response), response.Code);

                    var block2 = response.Options.Get<Block2>();

                    if (block2.BlockNumber != _readBlockNumber)
                        throw new CoapBlockException("Received incorrect block number from remote host");

                    _readBlockNumber++;

                    _reader.Enqueue(response.Payload, 0, response.Payload.Length);
                    _readerEvent.Set();

                    if (!response.Options.Get<Block2>().IsMoreFollowing)
                    {
                        EndOfStream = true;
                        Context.Response = response;
                    }
                }
            }
            catch (Exception ex)
            {
                // Hold onto the exception to throw it from a synchronous call.
                CaughtException = ex;
            }
            finally
            {
                EndOfStream = true;
                _readerEvent.Set();
            }
        }

        /// <summary>
        /// Attempt to flush any blocks to <see cref="CoapClient"/> that have been queued up.
        /// </summary>
        /// <inheritdoc/>
        public override void Flush()
        {
            ThrowExceptionIfCaught();
        }

        /// <summary>
        /// Attempt to flush any blocks to <see cref="CoapClient"/> that have been queued up.
        /// </summary>
        /// <inheritdoc/>
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await FlushFinishedEvent.WaitAsync(cancellationToken);

            ThrowExceptionIfCaught();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = 0;
            while (read < count && !cancellationToken.IsCancellationRequested)
            {
                if (!EndOfStream)
                    await _readerEvent.WaitAsync(cancellationToken);

                var bytesDequeued = _reader.Dequeue(buffer, offset + read, count - read);
                read += bytesDequeued;

                if (bytesDequeued == 0 && EndOfStream)
                    break;
            }

            return read;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThrowExceptionIfCaught();

                EndOfStream = true;
            }

            base.Dispose(disposing);
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

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        #endregion
    }
}
