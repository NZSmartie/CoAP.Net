using CoAPNet.Options;
using CoAPNet.Tests.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoAPNet.Tests
{
    [TestFixture]
    public class CoapBlockMessageTests
    {
        /// <summary>
        /// Timout for any Tasks
        /// </summary>
        public readonly int MaxTaskTimeout = System.Diagnostics.Debugger.IsAttached ? -1 : 2000;

        [Test]
        [Category("[RFC7959] Section 2.5"), Category("Blocks")]
        public void WriteBlockWiseCoapMessage(
            [Values(16, 32, 64, 128, 256, 512, 1024)] int blockSize, 
            [Range(1, 2)] int blocks, 
            [Values] bool lastHalfblock)
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint>() { CallBase = true };

            // "lambda" for generating our pseudo payload
            Func<int, int, byte[]> byteRange = (a, b) => Enumerable.Range(a, b).Select(i => Convert.ToByte(i % (byte.MaxValue + 1))).ToArray();

            int totalBytes = (blocks * blockSize) + (lastHalfblock ? blockSize / 2 : 0);
            int totalBlocks = ((totalBytes - 1) / blockSize) + 1;

            var baseRequestMessage = new CoapMessage
            {
                Code = CoapMessageCode.Post,
                Type = CoapMessageType.Confirmable,
            };

            var baseResponseMessage = new CoapMessage
            {
                Code = CoapMessageCode.Continue,
                Type = CoapMessageType.Acknowledgement,
            };

            // Generate an expected packet and response for all block-wise requests
            for (var block = 0; block < totalBlocks; block++)
            {
                var bytes = Math.Min(totalBytes - (block * blockSize), blockSize);

                var expected = baseRequestMessage.Clone();
                expected.Id = block + 1;
                expected.Payload = byteRange(blockSize * block, bytes);
                expected.Options.Add(new Options.Block1(block, blockSize, block != (totalBlocks - 1)));

                var response = baseResponseMessage.Clone();
                response.Id = block + 1;
                response.Options.Add(new Options.Block1(block, blockSize, block != (totalBlocks - 1)));

                mockClientEndpoint
                    .Setup(c => c.MockSendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expected.ToBytes()))))
                    .Callback(() => mockClientEndpoint.Object.EnqueueReceivePacket(new CoapPacket { Payload = response.ToBytes() }))
                    .Returns(Task.CompletedTask)
                    .Verifiable($"Did not send block: {block}");
            }
            
            // Act
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.SetNextMessageId(1);
                using (var writer = new CoapBlockStream(client, baseRequestMessage) { BlockSize = blockSize })
                {
                    writer.Write(byteRange(0, totalBytes), 0, totalBytes);

                    writer.Flush();
                };
            }

            // Assert
            mockClientEndpoint.Verify();
        }

        public static IEnumerable WriteBlockWiseCoapMessage_RemoteReduceBlockSize_Data
        {
            get
            {
                foreach (var i in new[] { 16, 32, 64, 128, 256, 512, 1024 })
                {
                    for (var r = (i / 2); r >= 16; r /= 2)
                    {
                        yield return new TestCaseData(i, r, 1, false);
                        yield return new TestCaseData(i, r, 2, false);
                        yield return new TestCaseData(i, r, 1, true);
                        yield return new TestCaseData(i, r, 2, true);
                    }
                }
            }
        }

        [Category("[RFC7959] Section 2.5"), Category("Blocks")]
        [TestCaseSource(nameof(WriteBlockWiseCoapMessage_RemoteReduceBlockSize_Data))]
        public void WriteBlockWiseCoapMessage_RemoteReduceBlockSize(int initialBlockSize, int reducetoBlockSize, int blocks, bool lastHalfblock)
        {
            Assume.That(reducetoBlockSize < initialBlockSize, "Ignoring invalid test input");

            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint>() { CallBase = true };

            // "lambda" for generating our pseudo payload
            Func<int, int, byte[]> byteRange = (a, b) => Enumerable.Range(a, b).Select(i => Convert.ToByte(i % (byte.MaxValue + 1))).ToArray();

            int totalBytes = (blocks * initialBlockSize) + (lastHalfblock ? initialBlockSize / 2 : 0);
            int totalBlocks = ((totalBytes - 1) / reducetoBlockSize) + 1;
            int messageId = 2;

            var baseRequestMessage = new CoapMessage
            {
                Code = CoapMessageCode.Post,
                Type = CoapMessageType.Confirmable,
            };

            var baseResponseMessage = new CoapMessage
            {
                Code = CoapMessageCode.Continue,
                Type = CoapMessageType.Acknowledgement,
            };

            {
                var expected = baseRequestMessage.Clone();
                expected.Id = 1;
                expected.Payload = byteRange(0, initialBlockSize);
                expected.Options.Add(new Options.Block1(0, initialBlockSize, initialBlockSize < totalBytes));

                var response = baseResponseMessage.Clone();
                response.Id = 1;
                response.Options.Add(new Options.Block1(0, reducetoBlockSize, initialBlockSize < totalBytes));

                mockClientEndpoint
                    .Setup(c => c.MockSendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expected.ToBytes()))))
                    .Callback(() => mockClientEndpoint.Object.EnqueueReceivePacket(new CoapPacket { Payload = response.ToBytes() }))
                    .Returns(Task.CompletedTask)
                    .Verifiable($"Did not send block: 0");
            }

            // Generate an expected packet and response for all block-wise requests
            for (var block = initialBlockSize / reducetoBlockSize; block < totalBlocks; block++)
            {
                var bytes = Math.Min(totalBytes - (block * reducetoBlockSize), reducetoBlockSize);

                var expected = baseRequestMessage.Clone();
                expected.Id = messageId;
                expected.Payload = byteRange(reducetoBlockSize * block, bytes);
                expected.Options.Add(new Options.Block1(block, reducetoBlockSize, block != (totalBlocks - 1)));

                var response = baseResponseMessage.Clone();
                response.Id = messageId;
                response.Options.Add(new Options.Block1(block, reducetoBlockSize, block != (totalBlocks - 1)));

                mockClientEndpoint
                    .Setup(c => c.MockSendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expected.ToBytes()))))
                    .Callback(() => mockClientEndpoint.Object.EnqueueReceivePacket(new CoapPacket { Payload = response.ToBytes() }))
                    .Returns(Task.CompletedTask)
                    .Verifiable($"Did not send block: {block}");

                messageId++;
            }

            // Act
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.SetNextMessageId(1);
                using (var writer = new CoapBlockStream(client, baseRequestMessage) { BlockSize = initialBlockSize })
                {
                    writer.Write(byteRange(0, totalBytes), 0, totalBytes);

                    writer.Flush();
                };
            }

            // Assert
            mockClientEndpoint.Verify();
        }

        [Test]
        [Category("Blocks")]
        public void ReadCoapMessageBlocks()
        {
            Assert.Inconclusive("Not Implemented");
        }
    }
}
