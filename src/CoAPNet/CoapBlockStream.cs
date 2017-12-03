using CoAPNet.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoAPNet
{
    public class CoapBlockStream : Stream
    {
        private readonly CoapClient _client;

        private readonly ByteQueue _reader = new ByteQueue();
        private readonly Task _readerTask;

        private readonly ByteQueue _writer = new ByteQueue();
        private readonly Task _writerTask;
        private readonly AsyncAutoResetEvent _writerEvent = new AsyncAutoResetEvent(false);
        private int _writeBlockNumber = 0;

        private Exception _exception = null;

        private readonly AsyncAutoResetEvent _flushDoneEvent = new AsyncAutoResetEvent(false);

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public readonly CoapMessage _baseMessage;

        private bool _endOfStream = false;

        private int _blockSize = 1024;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(-1);

        // TODO: check blocksize for valid value in 16,32,...,1024
        public int BlockSize
        {
            get => _blockSize;
            set => _blockSize = (value <= _blockSize) ? value : throw new ArgumentOutOfRangeException($"Can not increase blocksize from {_blockSize} to {value}");
        }


        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }


        public CoapBlockStream(CoapClient client, CoapMessage baseMessage = null)
        {
            _client = client;
            _baseMessage = baseMessage;

            _writerTask = WriteBlocksAsync();
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
                    _flushDoneEvent.Set();
                }
            }
            catch (Exception ex)
            {
                // Hold onto the exception to throw it from a synchronous call.
                _exception = ex;
            }
            finally
            {
                _endOfStream = true;
                _flushDoneEvent.Set();
            }
        }

        public override void Flush()
        {
            if (_exception == null && !_writerTask.IsCompleted)
            {
                _writerEvent.Set();
                _flushDoneEvent.WaitAsync(CancellationToken.None).Wait();
            }

            ThrowCaughtException();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            _writerEvent.Set();

            await _flushDoneEvent.WaitAsync(cancellationToken);

            ThrowCaughtException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _writer.Enqueue(buffer, offset, count);
            _writerEvent.Set();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_exception == null && !_writerTask.IsCompleted)
                {
                    _endOfStream = true;

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

                ThrowCaughtException();
            }
            base.Dispose(disposing);
        }

        private void ThrowCaughtException()
        {
            if (_exception == null)
                return;

            var exception = _exception;
            _exception = null;
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }
}
