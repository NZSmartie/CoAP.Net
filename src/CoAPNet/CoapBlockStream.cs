using CoAPNet.Utils;
using System;
using System.Collections.Generic;
using System.IO;
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

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public readonly CoapMessage _baseMessage;

        private bool _endOfStream = false;

        // TODO: check blocksize for valid value in 16,32,...,1024
        public int BlockSize { get; set; } = 256;


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

            while (!token.IsCancellationRequested && !_endOfStream)
            {
                await _writerEvent.WaitAsync(token);

                while (_writer.Length > BlockSize || (_endOfStream && _writer.Length > 0))
                {
                    var message = _baseMessage.Clone();

                    message.Id = 0;
                    message.Options.Add(new Options.Block1(_writeBlockNumber++, BlockSize, _writer.Length > BlockSize));

                    message.Payload = new byte[_writer.Length < BlockSize ? _writer.Length : BlockSize];
                    _writer.Dequeue(message.Payload, 0, BlockSize);

                    await _client.SendAsync(message);
                }
            }
        }

        public override void Flush()
        {
            _writerEvent.Set();
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
                _endOfStream = true;

                if (_writer.Length <= BlockSize)
                    _writerEvent.Set();
                else
                    _cancellationTokenSource.Cancel();

                try
                {
                    _writerTask.Wait();
                }
                catch (AggregateException) { }

            }
            base.Dispose(disposing);
        }
    }
}
