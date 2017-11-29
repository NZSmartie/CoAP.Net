using CoAPNet.Options;
using CoAPNet.Tests.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
        [Category("Blocks")]
        public async Task WriteCoapMessageBlocks()
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint>() { CallBase = true };

            mockClientEndpoint
                .Setup(c => c.MockSendAsync(It.IsAny<CoapPacket>()))
                .Returns(Task.CompletedTask);

            var baseMessage = new CoapMessage
            {
                Id = 1,
                Code = CoapMessageCode.Get,
                Type = CoapMessageType.NonConfirmable,
            };

            var expected1 = baseMessage.Clone();
            expected1.Id = 1;
            expected1.Payload = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f };
            expected1.Options.Add(new Options.Block1(0, 16, true));

            var expected2 = baseMessage.Clone();
            expected2.Id = 2;
            expected2.Payload = new byte[] { 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f };
            expected2.Options.Add(new Options.Block1(1, 16, true));

            var expected3 = baseMessage.Clone();
            expected3.Id = 3;
            expected3.Payload = new byte[] { 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27};
            expected3.Options.Add(new Options.Block1(2, 16, false));

            // Act
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.SetNextMessageId(1);
                using (var writer = new CoapBlockStream(client, baseMessage) { BlockSize = 16 })
                {
                    writer.Write(new byte[] 
                    {
                        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
                        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f,
                        0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27
                    }, 0, 40);
                };
            }

            // Assert
            mockClientEndpoint.Verify(cep => cep.SendAsync(It.Is<CoapPacket>(c => c.Payload.SequenceEqual(expected1.ToBytes()))), Times.Once, "Did not send first block");
            mockClientEndpoint.Verify(cep => cep.SendAsync(It.Is<CoapPacket>(c => c.Payload.SequenceEqual(expected2.ToBytes()))), Times.Once, "Did not send second block");
            mockClientEndpoint.Verify(cep => cep.SendAsync(It.Is<CoapPacket>(c => c.Payload.SequenceEqual(expected3.ToBytes()))), Times.Once, "Did not send third block");
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
