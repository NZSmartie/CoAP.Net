using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace CoAPNet.Options
{
    public class BlockBase : CoapOption
    {
        internal static readonly List<Tuple<int, int>> SupportedBlockSizes = new List<Tuple<int, int>>
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

        public BlockBase(int optionNumber, int blockNumber, int blockSize, bool more) : this(optionNumber)
        {
            BlockNumber = blockNumber;
            BlockSize = blockSize;
            IsMoreFollowing = more;
        }

        internal BlockBase(int optionNumber)
            :base (optionNumber, 0, 3, false, OptionType.UInt)
        { }

        private int _blockSize = 1024;

        public int BlockSize
        {
            get => _blockSize;
            set
            {
                if (!SupportedBlockSizes.Any(b => b.Item2 == value))
                    throw new ArgumentOutOfRangeException($"Unsupported blocksize {value}. Expecting block sizes in ({string.Join(", ", Options.BlockBase.SupportedBlockSizes.Select(b => b.Item2))})");
                _blockSize = value;
            }
        }

        public bool IsMoreFollowing { get; set; }

        private int _blockNumber = 0;

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

        public override void FromBytes(byte[] data)
        {
            base.FromBytes(data);
            uint last;

            if (data.Length == 0)
            {
                BlockNumber = 0;
                last = 0;
            }
            else if (data.Length == 1)
            {
                BlockNumber = (int)ValueUInt & 0x0F;
                last = (ValueUInt & 0xF0) >> 4;
            }
            else if (data.Length == 2)
            {
                BlockNumber = (int)ValueUInt & 0x0FFF;
                last = (ValueUInt & 0xF000) >> 12;
            }
            else if (data.Length == 3)
            {
                BlockNumber = (int)ValueUInt & 0x0FFFFF;
                last = (ValueUInt & 0xF00000) >> 20;
            }
            else
            {
                throw new CoapOptionException($"Invalid length ({data.Length}) of Block1/Block2 option");
            }

            IsMoreFollowing = (last & 0x01) > 0;
            var szx = (int)((last >> 1));
            BlockSize = SupportedBlockSizes.First(b => b.Item1 == szx).Item2;
        }

        public override byte[] GetBytes()
        {
            var szx = SupportedBlockSizes.First(b => b.Item2 == BlockSize).Item1;
            var last = (byte)((szx & 0x07) << 5) | (IsMoreFollowing ? 0x10 : 0x00);

            if (BlockNumber <= 0x0F)
            {
                ValueUInt = (uint)(last | (BlockNumber & 0x0F));
            }
            else if (BlockNumber <= 0x0FFF)
            {
                ValueUInt = (uint)((last << 8) | (BlockNumber & 0x0FFF));
            }
            else if (BlockNumber <= 0x0FFFFF)
            {
                ValueUInt = (uint)((last << 16) | (BlockNumber & 0x0FFFFF));
            }

            return base.GetBytes();
        }
    }

    public sealed class Block1 : BlockBase
    {
        /// <summary>
        /// Gets or Sets the block number in in the request <see cref="CoapMessage"/>
        /// </summary>
        /// <remarks>
        /// This value indicates which block the <see cref="CoapMessage.Payload"/> belongs to in the requesting message body.
        /// </remarks>
        public override int BlockNumber { get => base.BlockNumber; set => base.BlockNumber = value; }

        public Block1()
            : base(CoapRegisteredOptionNumber.Block1)
        { }

        public Block1(int blockNumber = 0, int blockSize = 256, bool more = false)
            : base(CoapRegisteredOptionNumber.Block1, blockNumber, blockSize, more)
        { }
    }

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

        public Block2() 
            : base(CoapRegisteredOptionNumber.Block2)
        { }

        public Block2(int blockNumber = 0, int blockSize = 256, bool more = false)
            : base(CoapRegisteredOptionNumber.Block2, blockNumber, blockSize, more)
        { }
    }

    public sealed class Size2 : CoapOption
    {
        public Size2() : base(CoapRegisteredOptionNumber.Size2, 0, 4, false, OptionType.UInt, 0u)
        { }

        public Size2(uint value) : this()
        {
            ValueUInt = value;
        }
    }
}
