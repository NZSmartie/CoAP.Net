using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace CoAPNet.Options
{
    public class BlockBase : CoapOption
    {
        internal List<Tuple<int, int>> SupportedBlockSizes = new List<Tuple<int, int>>
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
            SetStuff(blockNumber, SupportedBlockSizes.First(b => b.Item2 == blockSize).Item1, more);
        }

        internal BlockBase(int optionNumber)
            :base (optionNumber, 0, 3, false, OptionType.UInt)
        { }

        public int BlockSize
        {
            get
            {
                var n = 0;
                if (_length == 1)
                    n = (int)(ValueUInt & 0xE0) >> 5;
                if (_length == 2)
                    n = (int)(ValueUInt & 0xE000) >> 13;
                if (_length == 3)
                    n = (int)(ValueUInt & 0xE00000) >> 21;

                return SupportedBlockSizes.First(b => b.Item1 == n).Item2;
            }
            set
            {
                SetStuff(BlockNumber, SupportedBlockSizes.First(b => b.Item2 == value).Item1, IsMoreFollowing);
            }
        }

        public bool IsMoreFollowing
        {
            get
            {
                if (_length == 0)
                    return false;
                if (_length == 1)
                    return (ValueUInt & 0x10) != 0;
                if (_length == 2)
                    return (ValueUInt & 0x1000) != 0;
                else if (_length == 3)
                    return (ValueUInt & 0x100000) != 0;
                throw new ArgumentOutOfRangeException();
            }
            set
            {
                SetStuff(BlockNumber, BlockSize, value);
            }
        }

        public int BlockNumber
        {
            get
            {
                if (_length == 0)
                    return 0;
                if (_length == 1)
                    return (int)(ValueUInt & 0x0F);
                if (_length == 2)
                    return (int)(ValueUInt & 0x0FFF);
                else if (_length == 3)
                    return (int)(ValueUInt & 0x0FFFFF);
                throw new ArgumentOutOfRangeException();
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("Can not be less than 0");
                if (value > 0x0FFFFF)
                    throw new ArgumentOutOfRangeException("Can not be larger than 1,048,575 (2^20 - 1)");

                SetStuff(value, BlockSize, IsMoreFollowing);
            }
        }

        internal void SetStuff(int number, int size, bool more)
        {
            var last = ((size & 0x07) << 5) | (more ? 0x10 : 0x00);

            if (number <= 0x0F)
            {
                ValueUInt = (uint)(last | (number & 0x0F)) & 0xFF;
            }
            else if (number <= 0x0FFF)
            {
                ValueUInt = (uint)((last << 8) | (number & 0x0FFF)) & 0xFFFF;
            }
            else if (number <= 0x0FFFFF)
            {
                ValueUInt = (uint)((last << 16) | (number & 0x0FFFFF)) & 0xFFFFFF;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }

    public sealed class Block1 : BlockBase
    {
        public Block1()
            : base(CoapRegisteredOptionNumber.Block1)
        { }

        public Block1(int blockNumber = 0, int blockSize = 256, bool more = false)
            : base(CoapRegisteredOptionNumber.Block1, blockNumber, blockSize, more)
        { }
    }

    public sealed class Block2 : BlockBase
    {
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
