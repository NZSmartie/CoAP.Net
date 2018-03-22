using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Moq;

using CoAPNet.Tests.Mocks;
using System.Threading.Tasks;
using System.Threading;
using System.Linq.Expressions;

namespace CoAPNet.Tests.Utils
{
    internal class BlockWiseTestHelper
    {
        public int BlockSize { get; set; }

        public int TotalBytes { get; set; }

        public int TotalBlocks => ((TotalBytes - 1) / BlockSize) + 1;

        private bool IsBareRequest(CoapPacket packet, byte[] payload)
        {
            var message = CoapMessage.CreateFromBytes(packet.Payload);

            if (!message.Code.IsRequest())
                return false;

            if (message.Options.Get<Options.Block1>() != null)
                return false;

            if (message.Options.Get<Options.Block2>() != null)
                return false;

            if (payload != null && !payload.SequenceEqual(message.Payload))
                return false;

            return true;
        }

        private bool IsBlockSequence(CoapPacket packet, int blockNumber, int blockSize, bool hasMore, byte[] payload)
        {
            var message = CoapMessage.CreateFromBytes(packet.Payload);
            var block = message.Options.Get<Options.Block1>() as Options.BlockBase
                ?? message.Options.Get<Options.Block2>();

            if (block is Options.Block1)
            {
                if (!block.Equals(new Options.Block1(blockNumber, blockSize, hasMore)))
                    return false;
                if (payload != null && !payload.SequenceEqual(message.Payload))
                    return false;
            }
            else if (block is Options.Block2)
            {
                if (!block.Equals(new Options.Block2(blockNumber, blockSize, hasMore)))
                    return false;
            }

            return true;
        }

        public BlockWiseTestHelper AssertWriteRequestCorrespondance<TEndpoint>(Mock<TEndpoint> mockClientEndpoint, int startingBlock = 0) where TEndpoint : MockEndpoint
        {
            
            // Generate an expected packet and response for all block-wise requests
            for (var block = startingBlock; block < TotalBlocks; block++)
            {
                var bytes = ByteRange(BlockSize * block, Math.Min(TotalBytes - (block * BlockSize), BlockSize));
                // Make local copies of values as the expression below is evaluated later.
                int blockNumber = block, 
                    blockSize = BlockSize;
                var hasMore = block != (TotalBlocks - 1);

                mockClientEndpoint
                    .Setup(c => c.MockSendAsync(
                        It.Is<CoapPacket>(p => IsBlockSequence(p, blockNumber, blockSize, hasMore, bytes)), 
                        It.IsAny<CancellationToken>()))
                    .CallBase()
                    .Verifiable($"Did not send block: {block}");
            }

            return this;
        }

        public BlockWiseTestHelper AssertLargeBlockSizeGetsSent<TEndpoint>(Mock<TEndpoint> mockClientEndpoint, int initialBlockSize, int reducetoBlockSize = int.MaxValue) where TEndpoint : MockEndpoint
        {
            do
            {
                // Make local copies of values as the expression below is evaluated later.

                var bytes = ByteRange(0, initialBlockSize);
                // Make local copies of values as the expression below is evaluated later.
                int blockNumber = 0,
                    blockSize = initialBlockSize;
                var hasMore = blockSize < TotalBytes;

                mockClientEndpoint
                    .Setup(c => c.MockSendAsync(
                        It.Is<CoapPacket>(p => IsBlockSequence(p, blockNumber, blockSize, hasMore, bytes)),
                        It.IsAny<CancellationToken>()))
                    .CallBase()
                    .Verifiable($"Did not send initial too large block");

                initialBlockSize /= 2;
            }
            while (initialBlockSize > reducetoBlockSize);

            return this;
        }

        public BlockWiseTestHelper AssertReadResponseCorrespondance<TEndpoint>(Mock<TEndpoint> mockClientEndpoint, int startingBlock = 0) where TEndpoint : MockEndpoint
        {
            // Generate an expected packet and response for all block-wise requests
            for (var block = startingBlock; block < TotalBlocks; block++)
            {
                var blockNumber = block;
                mockClientEndpoint
                    .Setup(c => c.MockSendAsync(
                        It.Is<CoapPacket>(p => IsBlockSequence(p, blockNumber, BlockSize, false, null)),
                        It.IsAny<CancellationToken>()))
                    .CallBase()
                    .Verifiable($"Did not request block: {block}");
            }

            return this;
        }

        public BlockWiseTestHelper AssertInitialRequest<TEndpoint>(Mock<TEndpoint> mockClientEndpoint) where TEndpoint : MockEndpoint
        {
            mockClientEndpoint
                .Setup(c => c.MockSendAsync(
                    It.Is<CoapPacket>(p => IsBareRequest(p, null)),
                    It.IsAny<CancellationToken>()))
                .CallBase()
                .Verifiable($"Did not send initial request");

            return this;
        }

        public static byte[] ByteRange(int a, int b)
            => Enumerable.Range(a, b).Select(i => Convert.ToByte(i % (byte.MaxValue + 1))).ToArray();
    }
}
