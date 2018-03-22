using CoAPNet.Options;
using CoAPNet.Tests.Mocks;
using CoAPNet.Tests.Utils;
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
            var baseResponse = new CoapMessage
            {
                Code = CoapMessageCode.Continue,
                Type = CoapMessageType.Acknowledgement,
            };

            var baseRequest = new CoapMessage
            {
                Code = CoapMessageCode.Post,
                Type = CoapMessageType.Confirmable,
            };

            var totalBytes = (blocks * blockSize) + (lastHalfblock ? blockSize / 2 : 0);
            var totalBlocks = ((totalBytes - 1) / blockSize) + 1;

            var helper = new BlockWiseTestHelper
            {
                BlockSize = blockSize,
                TotalBytes = totalBytes,
            };

            var mockClientEndpoint = new Mock<MockBlockwiseEndpoint>(baseResponse, blockSize, totalBytes) { CallBase = true };

            helper.AssertWriteRequestCorrespondance(mockClientEndpoint);
            
            // Act
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.SetNextMessageId(1);
                using (var writer = new CoapBlockStreamWriter(client, baseRequest) { BlockSize = blockSize })
                {
                    writer.Write(BlockWiseTestHelper.ByteRange(0, totalBytes), 0, totalBytes);

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
            int totalBytes = (blocks * initialBlockSize) + (lastHalfblock ? initialBlockSize / 2 : 0);
            int totalBlocks = ((totalBytes - 1) / reducetoBlockSize) + 1;

            var baseRequest = new CoapMessage
            {
                Code = CoapMessageCode.Post,
                Type = CoapMessageType.Confirmable,
            };

            var baseResponse = new CoapMessage
            {
                Code = CoapMessageCode.Continue,
                Type = CoapMessageType.Acknowledgement,
            };

            var helper = new BlockWiseTestHelper
            {
                BlockSize = reducetoBlockSize,
                TotalBytes = totalBytes,
            };

            var mockClientEndpoint = new Mock<MockBlockwiseEndpoint>(baseResponse, reducetoBlockSize, totalBytes) { CallBase = true };

            helper.AssertLargeBlockSizeGetsSent(mockClientEndpoint, initialBlockSize)
                  .AssertWriteRequestCorrespondance(mockClientEndpoint, initialBlockSize / reducetoBlockSize);

            // Act
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.SetNextMessageId(1);
                using (var writer = new CoapBlockStreamWriter(client, baseRequest) { BlockSize = initialBlockSize })
                {
                    writer.Write(BlockWiseTestHelper.ByteRange(0, totalBytes), 0, totalBytes);

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
            var totalBytes = (blocks * initialBlockSize) + (lastHalfblock ? initialBlockSize / 2 : 0);
            var totalBlocks = ((totalBytes - 1) / reducetoBlockSize) + 1;

            var baseRequest = new CoapMessage
            {
                Code = CoapMessageCode.Post,
                Type = CoapMessageType.Confirmable,
            };

            var baseResponseMessage = new CoapMessage
            {
                Code = CoapMessageCode.Continue,
                Type = CoapMessageType.Acknowledgement,
            };

            var helper = new BlockWiseTestHelper
            {
                BlockSize = reducetoBlockSize,
                TotalBytes = totalBytes,
            };

            var mockClientEndpoint = new Mock<MockBlockwiseEndpoint>(baseRequest, reducetoBlockSize, totalBytes) { CallBase = true };
            mockClientEndpoint.Object.Mode = MockBlockwiseEndpointMode.RequestTooLarge;

            helper.AssertLargeBlockSizeGetsSent(mockClientEndpoint, initialBlockSize, reducetoBlockSize)
                  .AssertWriteRequestCorrespondance(mockClientEndpoint);

            // Act
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.SetNextMessageId(1);
                using (var writer = new CoapBlockStreamWriter(client, baseRequest) { BlockSize = initialBlockSize })
                {
                    writer.Write(BlockWiseTestHelper.ByteRange(0, totalBytes), 0, totalBytes);

                    writer.Flush();
                };
            }

            // Assert
            mockClientEndpoint.Verify();
        }

        [Test]
        [Category("[RFC7959] Section 2.4"), Category("Blocks")]
        public async Task Read_BlockWiseCoapMessage(
            [Values(16, 32, 64, 128, 256, 512, 1024)] int blockSize,
            [Range(1, 2)] int blocks,
            [Values] bool lastHalfblock)
        {
            // Arrange
            int totalBytes = (blocks * blockSize) + (lastHalfblock ? blockSize / 2 : 0);
            int totalBlocks = ((totalBytes - 1) / blockSize) + 1;

            var baseRequestMessage = new CoapMessage
            {
                Code = CoapMessageCode.Get,
                Type = CoapMessageType.Confirmable,
            };

            baseRequestMessage.SetUri("/status", UriComponents.Path);

            var baseResponse = new CoapMessage
            {
                Code = CoapMessageCode.Content,
                Type = CoapMessageType.Acknowledgement,
            };

            var helper = new BlockWiseTestHelper
            {
                BlockSize = blockSize,
                TotalBytes = totalBytes
            };

            var mockClientEndpoint = new Mock<MockBlockwiseEndpoint>(baseResponse, blockSize, totalBytes) { CallBase = true };

            helper.AssertReadResponseCorrespondance(mockClientEndpoint, 1)
                  .AssertInitialRequest(mockClientEndpoint);

            var result = new byte[totalBytes];
            int bytesRead;
            // Act
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.SetNextMessageId(1);

                var identifier = await client.SendAsync(baseRequestMessage, ct.Token);

                var response = await client.GetResponseAsync(identifier, ct.Token);

                using (var reader = new CoapBlockStreamReader(client, response, baseRequestMessage))
                {
                    bytesRead = reader.Read(result, 0, totalBytes);
                };
            }

            // Assert
            Assert.That(bytesRead, Is.EqualTo(totalBytes), "Incorrect number of bytes read");
            Assert.That(result, Is.EqualTo(BlockWiseTestHelper.ByteRange(0, totalBytes)), "Incorrect payload read");

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
            int totalBytes = (blocks * blockSize) + (lastHalfblock ? blockSize / 2 : 0);
            int totalBlocks = ((totalBytes - 1) / blockSize) + 1;

            var baseRequest = new CoapMessage
            {
                Code = CoapMessageCode.Get,
                Type = CoapMessageType.Confirmable,
            };

            baseRequest.SetUri("/status", UriComponents.Path);

            var baseResponse = new CoapMessage
            {
                Code = CoapMessageCode.Content,
                Type = CoapMessageType.Acknowledgement,
            };

            var helper = new BlockWiseTestHelper
            {
                BlockSize = blockSize,
                TotalBytes = totalBytes
            };

            var mockClientEndpoint = new Mock<MockBlockwiseEndpoint>(baseResponse, blockSize, totalBytes) { CallBase = true };

            helper.AssertReadResponseCorrespondance(mockClientEndpoint, 1)
                  .AssertInitialRequest(mockClientEndpoint);

            byte[] result;
            // Act
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.SetNextMessageId(1);

                var identifier = await client.SendAsync(baseRequest, ct.Token);

                var response = await client.GetResponseAsync(identifier, ct.Token);

                result = response.GetCompletedBlockWisePayload(client, baseRequest);
            }

            // Assert
            Assert.That(result, Is.EqualTo(BlockWiseTestHelper.ByteRange(0, totalBytes)), "Incorrect payload read");

            mockClientEndpoint.Verify();
        }

        [Test]
        [Category("[RFC7959] Section 2.5"), Category("Blocks")]
        public async Task Write_BlockWiseCoapMessage_ReadResponse()
        {
            int blockSize = 128;

            // Arrange
            var totalBytes = 1234;
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

            var mockClientEndpoint = new Mock<MockBlockwiseEndpoint>(baseResponseMessage, blockSize, totalBytes) { CallBase = true };
            mockClientEndpoint.Object.FinalResponse = new CoapMessage
            {
                Code = CoapMessageCode.Changed,
                Type = CoapMessageType.Acknowledgement,
            };

            var result = new byte[totalBytes];
            int bytesRead;

            // Act
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                var context = baseRequestMessage.CreateBlockWiseContext(client);

                using (var writer = new CoapBlockStreamWriter(context) { BlockSize = blockSize })
                {
                    writer.Write(BlockWiseTestHelper.ByteRange(0, totalBytes), 0, totalBytes);
                    writer.Flush();
                };

                using (var reader = new CoapBlockStreamReader(context))
                {
                    bytesRead = reader.Read(result, 0, totalBytes);
                };
            }

            // Assert
            Assert.That(bytesRead, Is.EqualTo(totalBytes), "Incorrect number of bytes read");
            Assert.That(result, Is.EqualTo(BlockWiseTestHelper.ByteRange(0, totalBytes)), "Incorrect payload read");

            mockClientEndpoint.Verify();
        }

        [Test]
        public void SupportedBlockSizes()
        {
            Assert.That(BlockBase.SupportedBlockSizes, Is.EqualTo(new[] { 16, 32, 64, 128, 256, 512, 1024 }));
        }
    }
}
