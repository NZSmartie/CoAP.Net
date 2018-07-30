using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace CoAPNet.Options
{
    /// <summary>
    /// Base class used with <see cref="Block1"/> and <see cref="Block2"/> as both sublasses share similarities.
    /// </summary>
    public class BlockBase : CoapUintOption
    {
        internal static readonly List<Tuple<int, int>> InternalSupportedBlockSizes = new List<Tuple<int, int>>
        {
            Tuple.Create(0, 16),
            Tuple.Create(1, 32),
            Tuple.Create(2, 64),
            Tuple.Create(3, 128),
            Tuple.Create(4, 256),
            Tuple.Create(5, 512),
            Tuple.Create(6, 1024),
            // Value of 7, 2048 is reserved
        };

        /// <summary>
        /// A list of sizes that are supported (as defined in RFC 7959)
        /// </summary>
        public static readonly IReadOnlyList<int> SupportedBlockSizes = InternalSupportedBlockSizes.Select(t => t.Item2).ToList();

        /// <summary>
        /// Mostly for internal use unless a custom block-wise CoAP Option is requried.
        /// </summary>
        /// <param name="optionNumber">The CoAP Option number.</param>
        /// <param name="blockNumber">The current block number.</param>
        /// <param name="blockSize">The size of the Block-Wise transfer blocks</param>
        /// <param name="more">Flag to indicate or request more blocks.</param>
        protected BlockBase(int optionNumber, int blockNumber, int blockSize, bool more) : this(optionNumber)
        {
            BlockNumber = blockNumber;
            BlockSize = blockSize;
            IsMoreFollowing = more;
        }

        internal BlockBase(int optionNumber)
            :base (optionNumber, 0, 3, false)
        { }

        private int _blockSize = 1024;

        /// <summary>
        /// Gets or sets the block size. See <see cref="SupportedBlockSizes"/>
        /// </summary>
        public int BlockSize
        {
            get => _blockSize;
            set
            {
                if (!InternalSupportedBlockSizes.Any(b => b.Item2 == value))
                    throw new ArgumentOutOfRangeException($"Unsupported blocksize {value}. Expecting block sizes in ({string.Join(", ", Options.BlockBase.InternalSupportedBlockSizes.Select(b => b.Item2))})");
                _blockSize = value;
            }
        }

        /// <summary>
        /// Gets or sets a flag to indicate more blocks to follow in the transfer.
        /// </summary>
        public bool IsMoreFollowing { get; set; }

        private int _blockNumber = 0;

        /// <summary>
        /// Gets or Sets the current block number in the transfer.
        /// </summary>
        public virtual int BlockNumber
        {
            get => _blockNumber;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("Can not be less than 0");
                if (value > 0x0FFFFF)
                    throw new ArgumentOutOfRangeException("Can not be larger than 1,048,575 (2^20 - 1)");

                _blockNumber = value;
            }
        }

        /// <inheritdoc/>
        public override int Length
        {
            get
            {
                if (BlockNumber == 0 && BlockSize == 16 && IsMoreFollowing == false)
                    return 0;
                if (BlockNumber <= 0x0F)
                    return 1;
                if (BlockNumber <= 0x0FFF)
                    return 2;
                if (BlockNumber <= 0x0FFFFF)
                    return 3;
                throw new InvalidOperationException();
            }
        }

        public override void Decode(Stream stream, int length)
        {
            base.Decode(stream, length);

            uint last;

            if (length == 0)
            {
                BlockNumber = 0;
                last = 0;
            }
            else if (length <= 3)
            {
                BlockNumber = (int)(ValueUInt & 0xFFFFF0) >> 4;
                last = ValueUInt & 0x0F;
            }
            else
            {
                throw new CoapOptionException($"Invalid length ({length}) of Block1/Block2 option");
            }

            IsMoreFollowing = (last & 0x08) > 0;
            var szx = (int)((last & 0x07));
            BlockSize = InternalSupportedBlockSizes.First(b => b.Item1 == szx).Item2;
        }

        /// <inheritdoc/>
        public override void FromBytes(byte[] data)
        {
            Decode(new MemoryStream(data), data.Length);
        }

        /// <inheritdoc/>
        [Obsolete]
        public override byte[] GetBytes()
        {
            using (var ms = new MemoryStream())
            {
                Encode(ms);
                return ms.ToArray();
            }
        }

        public override void Encode(Stream stream)
        {
            var szx = InternalSupportedBlockSizes.First(b => b.Item2 == BlockSize).Item1;
            var last = (byte)((szx & 0x07) | (IsMoreFollowing ? 0x08 : 0x00));

            ValueUInt = (uint)(last | ((BlockNumber << 4) & 0xFFFFF0));

            base.Encode(stream);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if(obj is BlockBase other)
            {
                if (BlockNumber != other.BlockNumber)
                    return false;
                if (BlockSize!= other.BlockSize)
                    return false;
                if (IsMoreFollowing != other.IsMoreFollowing)
                    return false;

                return true;

            }
            return base.Equals(obj);
        }
    }

    /// <summary>
    /// Block1 CoAP Option (as Defined in RFC 7959), informs the server of an incomming block-wise transfer request.
    /// </summary>
    public sealed class Block1 : BlockBase
    {
        /// <summary>
        /// Gets or Sets the block number in in the request <see cref="CoapMessage"/>
        /// </summary>
        /// <remarks>
        /// This value indicates which block the <see cref="CoapMessage.Payload"/> belongs to in the requesting message body.
        /// </remarks>
        public override int BlockNumber { get => base.BlockNumber; set => base.BlockNumber = value; }

        /// <summary>
        /// Creates a new Block1 request CoAP option
        /// </summary>
        public Block1()
            : base(CoapRegisteredOptionNumber.Block1)
        { }

        /// <summary>
        /// Creates a new Block1 request CoAP option
        /// </summary>
        /// <param name="blockNumber">The current block number.</param>
        /// <param name="blockSize">The size of the Block-Wise transfer blocks</param>
        /// <param name="more">Flag to indicate or request more blocks.</param>
        public Block1(int blockNumber = 0, int blockSize = 256, bool more = false)
            : base(CoapRegisteredOptionNumber.Block1, blockNumber, blockSize, more)
        { }
    }

    /// <summary>
    /// Block2 CoAP Option (as Defined in RFC 7959), informs the client of an incomming block-wise transfer response.
    /// </summary>
    public sealed class Block2 : BlockBase
    {
        /// <summary>
        /// Gets or Sets the block number in in the respone <see cref="CoapMessage"/>
        /// </summary>
        /// <remarks>
        /// When the <see cref="CoapMessage"/>.<see cref="CoapMessage.Code"/> is a <see cref="CoapMessageCodeClass.Request"/>: The remote endpoint will response with the appropiate block for the message body.
        /// Otherwise, this value indicates which block the <see cref="CoapMessage.Payload"/> belongs to in the message body.
        /// </remarks>
        public override int BlockNumber { get => base.BlockNumber; set => base.BlockNumber = value; }

        /// <summary>
        /// Creates a new Block1 response CoAP option
        /// </summary>
        public Block2() 
            : base(CoapRegisteredOptionNumber.Block2)
        { }

        /// <summary>
        /// Creates a new Block2 response CoAP option
        /// </summary>
        /// <param name="blockNumber">The current block number.</param>
        /// <param name="blockSize">The size of the Block-Wise transfer blocks</param>
        /// <param name="more">Flag to indicate or request more blocks.</param>
        public Block2(int blockNumber = 0, int blockSize = 256, bool more = false)
            : base(CoapRegisteredOptionNumber.Block2, blockNumber, blockSize, more)
        { }
    }

    /// <summary>
    /// A CoAP Option (as defiend in RFC7959), represents a request for the content's size, or (as a response from the server) the final size of the content.
    /// </summary>
    public sealed class Size2 : CoapUintOption
    {
        /// <summary>
        /// Creates a new Size2 CoAP Option.
        /// </summary>
        public Size2() : base(CoapRegisteredOptionNumber.Size2, 0, 4, false, 0u)
        { }

        /// <summary>
        /// Creates a new Size2 CoAP Option.
        /// </summary>
        /// <param name="value">The final size of the content.</param>
        public Size2(uint value) : this()
        {
            ValueUInt = value;
        }
    }
}
