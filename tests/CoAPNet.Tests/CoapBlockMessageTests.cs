using CoAPNet.Options;
using CoAPNet.Tests.Mocks;
using Moq;
using NUnit.Framework;
using System;
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
        public void WriteBlockWiseCoapMessage([Values(16, 32, 64, 128, 256, 512, 1024)] int blockSize, [Range(1, 5)] int blocks, [Random(1, 1000, 1)] int additionalBytes)
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint>() { CallBase = true };

            // "lambda" for generating our pseudo payload
            Func<int, int, byte[]> byteRange = (a, b) => Enumerable.Range(a, b).Select(i => Convert.ToByte(i % (byte.MaxValue + 1))).ToArray();

            int totalBytes = (blocks * blockSize) + additionalBytes;
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
                var bytes = block == (totalBlocks - 1) ? (totalBytes % blockSize) : blockSize;

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
        
        [Test]
        [Category("Blocks")]
        public async Task ReadCoapMessageBlocks()
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint> { CallBase = true };

            var expected = new CoapMessage
            {
                Id = 0x1234,
                Type = CoapMessageType.Acknowledgement,
                Code = CoapMessageCode.Content,
                Options = new System.Collections.Generic.List<CoapOption>
                    {
                        new Options.ContentFormat(Options.ContentFormatType.ApplicationLinkFormat),
                        new Block2(0, 256, false)
                    },
                //                Payload = Encoding.UTF8.GetBytes("</.well-known/core>")
            };

            mockClientEndpoint
                .SetupSequence(c => c.MockReceiveAsync())
                .Returns(Task.FromResult(new CoapPacket { Payload = expected.ToBytes() }))
                .Throws(new CoapEndpointException("disposed"));

            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.SetNextMessageId(0x1234);
                // Sned message
                var messageId = await client.GetAsync("coap://example.com/.well-known/core", ct.Token);

                using (var reader = new CoapBlockStream(client))
                {
                    // Receive msssage
                    // await client.GetResponseAsync(messageId, ct.Token).ConfigureAwait(false);
                }
            }

            // Assert
            mockClientEndpoint.Verify(x => x.ReceiveAsync(), Times.AtLeastOnce);
        }
    }
}
