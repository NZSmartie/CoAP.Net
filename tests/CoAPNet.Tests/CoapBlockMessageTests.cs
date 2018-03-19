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
        public void Write_BlockWiseCoapMessage(
            [Values(16, 32, 64, 128, 256, 512, 1024)] int blockSize, 
            [Range(1, 2)] int blocks, 
            [Values] bool lastHalfblock)
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint>() { CallBase = true };

            var totalBytes = (blocks * blockSize) + (lastHalfblock ? blockSize / 2 : 0);
            var totalBlocks = ((totalBytes - 1) / blockSize) + 1;

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
                expected.Payload = ByteRange(blockSize * block, bytes);
                expected.Options.Add(new Options.Block1(block, blockSize, block != (totalBlocks - 1)));

                var response = baseResponseMessage.Clone();
                response.Id = block + 1;
                response.Options.Add(new Options.Block1(block, blockSize, block != (totalBlocks - 1)));

                mockClientEndpoint
                    .Setup(c => c.MockSendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expected.ToBytes())),It.IsAny<CancellationToken>()))
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
                    writer.Write(ByteRange(0, totalBytes), 0, totalBytes);

                    writer.Flush();
                };
            }

            // Assert
            mockClientEndpoint.Verify();
        }

        /// <summary>
        /// Generates parameters for <see cref="Write_BlockWiseCoapMessage_RemoteReduceBlockSize"/>
        /// </summary>
        public static IEnumerable Write_BlockWiseCoapMessage_RemoteReduceBlockSize_Data()
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

        [Category("[RFC7959] Section 2.5"), Category("Blocks")]
        [TestCaseSource(nameof(Write_BlockWiseCoapMessage_RemoteReduceBlockSize_Data))]
        public void Write_BlockWiseCoapMessage_RemoteReduceBlockSize(int initialBlockSize, int reducetoBlockSize, int blocks, bool lastHalfblock)
        {
            Assume.That(reducetoBlockSize < initialBlockSize, "Ignoring invalid test input");

            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint>() { CallBase = true };

            // "lambda" for generating our pseudo payload
            byte[] byteRange(int a, int b) => Enumerable.Range(a, b).Select(i => Convert.ToByte(i % (byte.MaxValue + 1))).ToArray();

            int totalBytes = (blocks * initialBlockSize) + (lastHalfblock ? initialBlockSize / 2 : 0);
            int totalBlocks = ((totalBytes - 1) / reducetoBlockSize) + 1;
            int messageId = 1;

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
                expected.Id = messageId;
                expected.Payload = byteRange(0, initialBlockSize);
                expected.Options.Add(new Options.Block1(0, initialBlockSize, initialBlockSize < totalBytes));

                var response = baseResponseMessage.Clone();
                response.Id = messageId++;
                response.Options.Add(new Options.Block1(0, reducetoBlockSize, initialBlockSize < totalBytes));

                mockClientEndpoint
                    .Setup(c => c.MockSendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expected.ToBytes())), It.IsAny<CancellationToken>()))
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
                response.Id = messageId++;
                response.Options.Add(new Options.Block1(block, reducetoBlockSize, block != (totalBlocks - 1)));

                mockClientEndpoint
                    .Setup(c => c.MockSendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expected.ToBytes())), It.IsAny<CancellationToken>()))
                    .Callback(() => mockClientEndpoint.Object.EnqueueReceivePacket(new CoapPacket { Payload = response.ToBytes() }))
                    .Returns(Task.CompletedTask)
                    .Verifiable($"Did not send block: {block}");
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

        [Category("[RFC7959] Section 2.5"), Category("Blocks")]
        [TestCaseSource(nameof(Write_BlockWiseCoapMessage_RemoteReduceBlockSize_Data))]
        public void Write_BlockWiseCoapMessage_BlockSizeTooLarge(int initialBlockSize, int reducetoBlockSize, int blocks, bool lastHalfblock)
        {
            Assume.That(reducetoBlockSize < initialBlockSize, "Ignoring invalid test input");

            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint>() { CallBase = true };

            var totalBytes = (blocks * initialBlockSize) + (lastHalfblock ? initialBlockSize / 2 : 0);
            var totalBlocks = ((totalBytes - 1) / reducetoBlockSize) + 1;
            var messageId = 1;

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

            // Generate an expected packet and RequestEntityTooLarge response
            for (var blockSize = initialBlockSize; blockSize > reducetoBlockSize; blockSize /= 2)
            {
                var expected = baseRequestMessage.Clone();
                expected.Id = messageId;
                expected.Payload = ByteRange(0, blockSize);
                expected.Options.Add(new Options.Block1(0, blockSize, blockSize < totalBytes));

                var response = new CoapMessage
                {
                    Id = messageId++,
                    Code = CoapMessageCode.RequestEntityTooLarge,
                    Type = CoapMessageType.Acknowledgement
                };

                mockClientEndpoint
                    .Setup(c => c.MockSendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expected.ToBytes())), It.IsAny<CancellationToken>()))
                    .Callback(() => mockClientEndpoint.Object.EnqueueReceivePacket(new CoapPacket { Payload = response.ToBytes() }))
                    .Returns(Task.CompletedTask)
                    .Verifiable($"Did not send blockSize ({blockSize}) attempt");
            }

            // Generate an expected packet and response for all block-wise requests
            for (var block = 0; block < totalBlocks; block++)
            {
                var bytes = Math.Min(totalBytes - (block * reducetoBlockSize), reducetoBlockSize);

                var expected = baseRequestMessage.Clone();
                expected.Id = messageId;
                expected.Payload = ByteRange(reducetoBlockSize * block, bytes);
                expected.Options.Add(new Options.Block1(block, reducetoBlockSize, block != (totalBlocks - 1)));

                var response = baseResponseMessage.Clone();
                response.Id = messageId++;
                response.Options.Add(new Options.Block1(block, reducetoBlockSize, block != (totalBlocks - 1)));

                mockClientEndpoint
                    .Setup(c => c.MockSendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expected.ToBytes())), It.IsAny<CancellationToken>()))
                    .Callback(() => mockClientEndpoint.Object.EnqueueReceivePacket(new CoapPacket { Payload = response.ToBytes() }))
                    .Returns(Task.CompletedTask)
                    .Verifiable($"Did not send block: {block}");
            }

            // Act
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.SetNextMessageId(1);
                using (var writer = new CoapBlockStream(client, baseRequestMessage) { BlockSize = initialBlockSize })
                {
                    writer.Write(ByteRange(0, totalBytes), 0, totalBytes);

                    writer.Flush();
                };
            }

            // Assert
            mockClientEndpoint.Verify();
        }

        public void Setup_Read_BlockWiseCorrespondance(
            Mock<MockEndpoint> mockClientEndpoint, 
            int messageId, 
            int totalBytes, 
            int blockSize,
            int totalBlocks, 
            CoapMessage baseRequestMessage, 
            CoapMessage baseResponseMessage)
        {
            {
                var block = 0;
                var bytes = Math.Min(totalBytes - (block * blockSize), blockSize);

                var expected = baseRequestMessage.Clone();
                expected.Id = messageId;

                var response = baseResponseMessage.Clone();
                response.Id = messageId++;
                response.Options.Add(new Options.Block2(block, blockSize, block != (totalBlocks - 1)));
                response.Payload = ByteRange(blockSize * block, bytes);

                Debug.WriteLine($"Expecting: {expected}");
                Debug.WriteLine($" Response: {response}");

                mockClientEndpoint
                    .Setup(c => c.MockSendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expected.ToBytes())), It.IsAny<CancellationToken>()))
                    .Callback(() => mockClientEndpoint.Object.EnqueueReceivePacket(new CoapPacket { Payload = response.ToBytes() }))
                    .Returns(Task.CompletedTask)
                    .Verifiable($"Did not send block: {block}");
            }

            // Generate an expected packet and response for all block-wise requests
            for (var block = 1; block < totalBlocks; block++)
            {
                var bytes = Math.Min(totalBytes - (block * blockSize), blockSize);

                var expected = baseRequestMessage.Clone();
                expected.Id = messageId;
                expected.Options.Add(new Options.Block2(block, blockSize));

                var response = baseResponseMessage.Clone();
                response.Id = messageId++;
                response.Options.Add(new Options.Block2(block, blockSize, block != (totalBlocks - 1)));
                response.Payload = ByteRange(blockSize * block, bytes);

                Debug.WriteLine($"Expecting: {expected}");
                Debug.WriteLine($" Response: {response}");

                mockClientEndpoint
                    .Setup(c => c.MockSendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expected.ToBytes())), It.IsAny<CancellationToken>()))
                    .Callback(() => mockClientEndpoint.Object.EnqueueReceivePacket(new CoapPacket { Payload = response.ToBytes() }))
                    .Returns(Task.CompletedTask)
                    .Verifiable($"Did not send block: {block}");
            }
        }

        [Test]
        [Category("[RFC7959] Section 2.4"), Category("Blocks")]
        public async Task Read_BlockWiseCoapMessage(
            [Values(16, 32, 64, 128, 256, 512, 1024)] int blockSize,
            [Range(1, 2)] int blocks,
            [Values] bool lastHalfblock)
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint>() { CallBase = true };

            var messageId = 1;

            // "lambda" for generating our pseudo payload

            int totalBytes = (blocks * blockSize) + (lastHalfblock ? blockSize / 2 : 0);
            int totalBlocks = ((totalBytes - 1) / blockSize) + 1;

            var baseRequestMessage = new CoapMessage
            {
                Code = CoapMessageCode.Get,
                Type = CoapMessageType.Confirmable,
            };

            baseRequestMessage.SetUri("/status", UriComponents.Path);

            var baseResponseMessage = new CoapMessage
            {
                Code = CoapMessageCode.Content,
                Type = CoapMessageType.Acknowledgement,
            };

            Setup_Read_BlockWiseCorrespondance(mockClientEndpoint, messageId, totalBytes, blockSize, totalBlocks, baseRequestMessage, baseResponseMessage);

            var result = new byte[totalBytes];
            int bytesRead;
            // Act
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.SetNextMessageId(1);

                var identifier = await client.SendAsync(baseRequestMessage, ct.Token);

                var response = await client.GetResponseAsync(identifier, ct.Token);

                using (var reader = new CoapBlockStream(client, response, baseRequestMessage))
                {
                    bytesRead = reader.Read(result, 0, totalBytes);
                };
            }

            // Assert
            Assert.That(bytesRead, Is.EqualTo(totalBytes), "Incorrect number of bytes read");
            Assert.That(result, Is.EqualTo(ByteRange(0, totalBytes)), "Incorrect payload read");

            mockClientEndpoint.Verify();
        }

        [Test]
        [Category("[RFC7959] Section 2.4"), Category("Blocks")]
        public async Task Read_BlockWiseCoapMessage_WithExtentionMethod(
            [Values(16, 32, 64, 128, 256, 512, 1024)] int blockSize,
            [Range(1, 2)] int blocks,
            [Values] bool lastHalfblock)
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint>() { CallBase = true };

            var messageId = 1;

            // "lambda" for generating our pseudo payload

            int totalBytes = (blocks * blockSize) + (lastHalfblock ? blockSize / 2 : 0);
            int totalBlocks = ((totalBytes - 1) / blockSize) + 1;

            var baseRequestMessage = new CoapMessage
            {
                Code = CoapMessageCode.Get,
                Type = CoapMessageType.Confirmable,
            };

            baseRequestMessage.SetUri("/status", UriComponents.Path);

            var baseResponseMessage = new CoapMessage
            {
                Code = CoapMessageCode.Content,
                Type = CoapMessageType.Acknowledgement,
            };

            Setup_Read_BlockWiseCorrespondance(mockClientEndpoint, messageId, totalBytes, blockSize, totalBlocks, baseRequestMessage, baseResponseMessage);

            byte[] result;
            // Act
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.SetNextMessageId(1);

                var identifier = await client.SendAsync(baseRequestMessage, ct.Token);

                var response = await client.GetResponseAsync(identifier, ct.Token);

                result = response.GetCompletedBlockWisePayload(client, baseRequestMessage);
            }

            // Assert
            Assert.That(result, Is.EqualTo(ByteRange(0, totalBytes)), "Incorrect payload read");

            mockClientEndpoint.Verify();
        }

        private byte[] ByteRange(int a, int b) 
            => Enumerable.Range(a, b).Select(i => Convert.ToByte(i % (byte.MaxValue + 1))).ToArray();

        [Test]
        public void SupportedBlockSizes()
        {
            Assert.That(BlockBase.SupportedBlockSizes, Is.EqualTo(new[] { 16, 32, 64, 128, 256, 512, 1024 }));
        }
    }
}
