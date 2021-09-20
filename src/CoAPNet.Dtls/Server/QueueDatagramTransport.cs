using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Org.BouncyCastle.Crypto.Tls;

namespace CoAPNet.Dtls.Server
{
    /// <summary>
    /// We need this because the server can't bind to a single endpoint on the server port, so we have to receive packets from all
    /// endpoints and pass them to the corresponsing "connection" / transport by the remote endpoint.
    /// </summary>
    internal class QueueDatagramTransport : DatagramTransport
    {
        private readonly int _receiveLimit;
        private readonly int _sendLimit;
        private readonly Action<byte[]> _sendCallback;
        private readonly CancellationTokenSource _cts;

        private const int MIN_IP_OVERHEAD = 20;
        private const int MAX_IP_OVERHEAD = MIN_IP_OVERHEAD + 64;
        private const int UDP_OVERHEAD = 8;

        public QueueDatagramTransport(int mtu, Action<byte[]> sendCallback)
        {
            _receiveLimit = mtu - MIN_IP_OVERHEAD - UDP_OVERHEAD;
            _sendLimit = mtu - MAX_IP_OVERHEAD - UDP_OVERHEAD;
            _sendCallback = sendCallback ?? throw new ArgumentNullException(nameof(sendCallback));
            _cts = new CancellationTokenSource();
        }

        public BlockingCollection<byte[]> ReceiveQueue { get; internal set; } = new BlockingCollection<byte[]>();
        public bool IsClosed { get; private set; }
        public ReaderWriterLockSlim CloseLock { get; } = new ReaderWriterLockSlim();

        public void Close()
        {
            _cts.Cancel();

            CloseLock.EnterWriteLock();
            IsClosed = true;
            CloseLock.ExitWriteLock();
            ReceiveQueue.Dispose();
        }

        public int GetReceiveLimit()
        {
            return _receiveLimit;
        }

        public int GetSendLimit()
        {
            return _sendLimit;
        }

        public int Receive(byte[] buf, int off, int len, int waitMillis)
        {
            try
            {
                CloseLock.EnterReadLock();
                if (IsClosed)
                    throw new DtlsConnectionClosedException();

                var success = ReceiveQueue.TryTake(out var data, waitMillis, _cts.Token);
                if (!success)
                    return -1; // DO NOT return 0. This will disable the wait timeout effectively for the caller and any abort logic will by bypassed!
                var readLen = Math.Min(len, data.Length);
                Array.Copy(data, 0, buf, off, readLen);
                return readLen;
            }
            catch (OperationCanceledException)
            {
                return -1; // DO NOT return 0. This will disable the wait timeout effectively for the caller and any abort logic will by bypassed!
            }
            finally
            {
                CloseLock.ExitReadLock();
            }
        }

        public void Send(byte[] buf, int off, int len)
        {
            try
            {
                CloseLock.EnterReadLock();
                if (IsClosed)
                    throw new DtlsConnectionClosedException(); // throw is important here, so DtlsServer.Accept() throws when the connection is closed and the client doesn't answer
                _sendCallback(new ArraySegment<byte>(buf, off, len).ToArray());
            }
            finally
            {
                CloseLock.ExitReadLock();
            }
        }
    }
}
