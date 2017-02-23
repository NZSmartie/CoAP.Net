using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using CoAP.Net;

namespace CoAP.Net.Tests
{
    [TestClass]
    public class Client
    {

        /// <summary>
        /// Timout for any Tasks
        /// </summary>
        public const int MaxTaskTimeout = 2000;

        [TestMethod]
        public void TestClientRequest()
        {
            // Arrange
            var mockClientEndpoint = new Mock<ICoapEndpoint>();

            mockClientEndpoint
                .Setup(c => c.SendAsync(It.IsAny<CoapPayload>()))
                .Returns(Task.CompletedTask);

            // Act
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                client.SendAsync(new CoapMessage
                {
                    Type = CoapMessageType.Confirmable,
                    Code = CoapMessageCode.None
                }).Wait();
            }

            // Assert
            mockClientEndpoint.Verify(cep => cep.SendAsync(It.IsAny<CoapPayload>()));
        }
        
        [TestMethod]
        public void TestClientResponse()
        {
            // Arrange
            var mockClientEndpoint = new Mock<ICoapEndpoint>();
            var mockPayload = new Mock<CoapPayload>();

            var expected = new CoapMessage
            {
                Type = CoapMessageType.Acknowledgement,
                Code = CoapMessageCode.Content,
                Options = new System.Collections.Generic.List<CoapOption>
                    {
                        new Options.ContentFormat(Options.ContentFormatType.ApplicationLinkFormat)
                    },
                Payload = System.Text.Encoding.UTF8.GetBytes("</.well-known/core>")
            };

            mockPayload
                .Setup(p => p.Payload)
                .Returns(() => expected.Serialise());
            mockClientEndpoint
                .Setup(c => c.SendAsync(It.IsAny<CoapPayload>()))
                // Copy the ID from the message sent out, to the message for the client to receive
                .Callback<CoapPayload>((p) => expected.Id = p.MessageId)
                .Returns(Task.CompletedTask);
            mockClientEndpoint
                .SetupSequence(c => c.ReceiveAsync())
                .Returns(Task.FromResult(mockPayload.Object))
                .Throws(new CoapEndpointException());

            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                // Sned message
                var sendTask = client.GetAsync("coap://example.com/.well-known/core");
                sendTask.Wait(MaxTaskTimeout);

                if (!sendTask.IsCompleted)
                    throw new AssertFailedException("sendTask took too long to complete");

                client.Listen(); // enable loop back thingy

                // Receive msssage
                var responseTask = client.GetResponseAsync(sendTask.Result);
                responseTask.Wait(MaxTaskTimeout);

                if (!responseTask.IsCompleted)
                    throw new AssertFailedException("responseTask took too long to complete");

            }

            // Assert
            mockClientEndpoint.Verify(x => x.ReceiveAsync(), Times.AtLeastOnce);
        }

        [TestMethod]
        public void TestClientOnMessageReceivedEvent()
        {
            // Arrange
            var expected = new CoapMessage
            {
                Id = 0x1234,
                Type = CoapMessageType.Acknowledgement,
                Code = CoapMessageCode.None,
            };
            var mockPayload = new Mock<CoapPayload>();
            var mockClientEndpoint = new Mock<ICoapEndpoint>();
            var clientOnMessageReceivedEventCalled = false;

            mockPayload
                .Setup(p => p.Payload)
                .Returns(expected.Serialise());
            mockClientEndpoint
                .SetupSequence(c => c.ReceiveAsync())
                .Returns(Task.FromResult(mockPayload.Object))
                .Throws(new CoapEndpointException());

            // Act
            using (var mockClient = new CoapClient(mockClientEndpoint.Object))
            {
                var task = new TaskCompletionSource<bool>();
                mockClient.OnMessageReceived += (s, e) =>
                {
                    clientOnMessageReceivedEventCalled = true;
                    task.SetResult(true);
                };

                mockClient.Listen();

                task.Task.Wait(MaxTaskTimeout);
                mockClient.Dispose();
            }

            // Assert
            Assert.IsTrue(clientOnMessageReceivedEventCalled);
        }
    }
}
