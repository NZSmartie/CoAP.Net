    #region Copyright & License
    /*************************************************************************
     * 
     * The MIT License (MIT)
     * 
     * Copyright (c) 2014 Roman Atachiants (kelindar@gmail.com)
     * Permission is hereby granted, free of charge, to any person obtaining a copy
     * of this software and associated documentation files (the "Software"), to deal
     * in the Software without restriction, including without limitation the rights
     * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
     * copies of the Software, and to permit persons to whom the Software is
     * furnished to do so, subject to the following conditions:
     * 
     * The above copyright notice and this permission notice shall be included in
     * all copies or substantial portions of the Software.
     * 
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
     * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
     * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
     * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
     * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
     * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
     * THE SOFTWARE.
    *************************************************************************/
    #endregion

    using System;
    using System.IO;
    using System.Runtime.InteropServices;

namespace CoAPNet.Utils
{
    /// <summary>
    /// Defines a class that represents a resizable circular byte queue.
    /// </summary>
    internal sealed class ByteQueue
    {


        private int _head;
        private int _tail;
        private int _size;
        private int _sizeUntilCut;
        private byte[] _buffer;
        private int _bufferSize = 2048; // must be a power of 2


        /// <summary>
        /// Gets the length of the byte queue
        /// </summary>
        public int Length
        {
            get { return _size; }
        }

        /// <summary>
        /// Constructs a new instance of a byte queue.
        /// </summary>
        public ByteQueue()
        {
            _buffer = new byte[_bufferSize];
        }

        /// <summary>
        /// Clears the byte queue
        /// </summary>
        internal void Clear()
        {
            _head = 0;
            _tail = 0;
            _size = 0;
            _sizeUntilCut = _buffer.Length;
        }

        /// <summary>
        /// Clears the byte queue
        /// </summary>
        internal void Clear(int size)
        {
            lock (this)
            {
                if (size > _size)
                    size = _size;

                if (size == 0)
                    return;

                _head = (_head + size) % _buffer.Length;
                _size -= size;

                if (_size == 0)
                {
                    _head = 0;
                    _tail = 0;
                }

                _sizeUntilCut = _buffer.Length - _head;
                return;
            }
        }

        /// <summary>
        /// Extends the capacity of the bytequeue
        /// </summary>
        private void SetCapacity(int capacity)
        {
            if ((capacity & (capacity - 1)) != 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), $"Capacity must be a power of two, {capacity} is invalid.");

            byte[] newBuffer = new byte[capacity];

            if (_size > 0)
            {
                if (_head < _tail)
                {
                    Buffer.BlockCopy(_buffer, _head, newBuffer, 0, _size);
                }
                else
                {
                    Buffer.BlockCopy(_buffer, _head, newBuffer, 0, _buffer.Length - _head);
                    Buffer.BlockCopy(_buffer, 0, newBuffer, _buffer.Length - _head, _tail);
                }
            }

            _head = 0;
            _tail = _size;
            _buffer = newBuffer;
            _bufferSize = capacity;
        }


        /// <summary>
        /// Enqueues a buffer to the queue and inserts it to a correct position
        /// </summary>
        /// <param name="buffer">Buffer to enqueue</param>
        /// <param name="offset">The zero-based byte offset in the buffer</param>
        /// <param name="size">The number of bytes to enqueue</param>
        internal void Enqueue(byte[] buffer, int offset, int size)
        {
            if (size == 0)
                return;

            lock (this)
            {
                if ((_size + size) > _buffer.Length)
                {
                    var capacity = (_size + size + (_bufferSize - 1)) & ~(_bufferSize - 1);
                    System.Diagnostics.Debug.WriteLine($"Resizing ByteQueue to {capacity}");
                    SetCapacity(capacity);
                }

                if (_head < _tail)
                {
                    int rightLength = (_buffer.Length - _tail);

                    if (rightLength >= size)
                    {
                        Buffer.BlockCopy(buffer, offset, _buffer, _tail, size);
                    }
                    else
                    {
                        Buffer.BlockCopy(buffer, offset, _buffer, _tail, rightLength);
                        Buffer.BlockCopy(buffer, offset + rightLength, _buffer, 0, size - rightLength);
                    }
                }
                else
                {
                    Buffer.BlockCopy(buffer, offset, _buffer, _tail, size);
                }

                _tail = (_tail + size) % _buffer.Length;
                _size += size;
                _sizeUntilCut = _buffer.Length - _head;
            }
        }

        /// <summary>
        /// Dequeues a buffer from the queue
        /// </summary>
        /// <param name="buffer">Buffer to enqueue</param>
        /// <param name="offset">The zero-based byte offset in the buffer</param>
        /// <param name="size">The number of bytes to dequeue</param>
        /// <returns>Number of bytes dequeued</returns>
        internal int Dequeue(byte[] buffer, int offset, int size)
        {
            lock (this)
            {
                size = PeekInternal(buffer, offset, size);
                AdvanceQueueInternal(size);
                return size;
            }
        }

        internal void AdvanceQueue(int bytes)
        {
            lock (this)
                AdvanceQueueInternal(bytes);
        }

        private void AdvanceQueueInternal(int bytes)
        {
            _head = (_head + bytes) % _buffer.Length;
            _size -= bytes;

            if (_size == 0)
            {
                _head = 0;
                _tail = 0;
            }

            _sizeUntilCut = _buffer.Length - _head;
        }

        /// <summary>
        /// Dequeues a buffer from the queue
        /// </summary>
        /// <param name="buffer">Buffer to enqueue</param>
        /// <param name="offset">The zero-based byte offset in the buffer</param>
        /// <param name="size">The number of bytes to dequeue</param>
        /// <returns>Number of bytes dequeued</returns>
        internal int Peek(byte[] buffer, int offset, int size)
        {
            lock (this)
                return PeekInternal(buffer, offset, size);
        }

        private int PeekInternal(byte[] buffer, int offset, int size)
        {
            if (size > _size)
                size = _size;

            if (size == 0)
                return 0;

            if (_head < _tail)
            {
                Buffer.BlockCopy(_buffer, _head, buffer, offset, size);
            }
            else
            {
                int rightLength = (_buffer.Length - _head);

                if (rightLength >= size)
                {
                    Buffer.BlockCopy(_buffer, _head, buffer, offset, size);
                }
                else
                {
                    Buffer.BlockCopy(_buffer, _head, buffer, offset, rightLength);
                    Buffer.BlockCopy(_buffer, 0, buffer, offset + rightLength, size - rightLength);
                }
            }

            return size;
        }

        /// <summary>
        /// Peeks a byte with a relative index to the fHead
        /// Note: should be used for special cases only, as it is rather slow
        /// </summary>
        /// <param name="index">A relative index</param>
        /// <returns>The byte peeked</returns>
        private byte PeekOne(int index)
        {
            return index >= _sizeUntilCut
                ? _buffer[index - _sizeUntilCut]
                : _buffer[_head + index];
        }


    }
}