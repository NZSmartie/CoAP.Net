using System;
using System.Linq;
using System.Threading;
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
        [TestCategory("CoapClient")]
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
        [TestCategory("CoapClient")]
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
        [TestCategory("CoapClient")]
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

        // ToDo: Test Reject Empty Message With Format Error
        [TestMethod]
        [TestCategory("[RFC7252] Section 4.1")]
        public void TestRejectEmptyMessageWithFormatError()
        {
            // Arrange
            var expected = new CoapMessage
            {
                Id = 0x1234,
                Type = CoapMessageType.Reset,
                Code = CoapMessageCode.None,
            };

            var mockPayload = new Mock<CoapPayload>();
            var mockClientEndpoint = new Mock<ICoapEndpoint>();

            mockPayload
                .Setup(p => p.Payload)
                .Returns(new byte[] { 0x40, 0x00, 0x12, 0x34, 0xFF, 0x12, 0x34 }); // "Empty" Message with a payload
            mockClientEndpoint
                .SetupSequence(c => c.ReceiveAsync())
                .Returns(Task.FromResult(mockPayload.Object))
                .Throws(new CoapEndpointException());

            // Act
            using (var mockClient = new CoapClient(mockClientEndpoint.Object))
            {
                mockClient.Listen();

                // Assert
                mockClientEndpoint.Verify(cep => cep.SendAsync(It.Is<CoapPayload>(p => p.Payload.SequenceEqual(expected.Serialise()))));
            }
        }

        // ToDo: Test Piggy Backed Response
        [TestMethod]
        [TestCategory("[RFC7252] Section 4.2")]
        public void TestPiggyBackedResponse()
        {
            throw new NotImplementedException();
        }

        // ToDo: Test Retransmit Delays
        [TestMethod]
        [TestCategory("[RFC7252] Section 4.2")]
        public void TestRetransmitDelays()
        {
            throw new NotImplementedException();
        }

        // ToDo: Test Ignore Repeated Messages
        [TestMethod]
        [TestCategory("[RFC7252] Section 4.2")]
        public void TestIgnoreRepeatedMessages()
        {
            throw new NotImplementedException();
        }

        // ToDo: Test Ignore Messages Received After Timeout
        [TestMethod]
        [TestCategory("[RFC7252] Section 4.2")]
        public void TestIgnoreMessagesReceivedAfterTimeout()
        {
            throw new NotImplementedException();
        }

        // ToDo: Test Ignore Non-Empty Reset Messages
        [TestMethod]
        [TestCategory("[RFC7252] Section 4.2")]
        public void TestIgnoreNonEmptyResetMessages()
        {
            throw new NotImplementedException();
        }

        // ToDo: Test Ignore Acknowledgement Messages With Reserved Code
        [TestMethod]
        [TestCategory("[RFC7252] Section 4.2")]
        public void TestIgnoreAcknowledgementMessagesWithReservedCode()
        {
            throw new NotImplementedException();
        }

        // Todo: Test Reached Max Failed Retransmit Attempts
        [TestMethod]
        [TestCategory("[RFC7252] Section 4.2")]
        public void TestReachedMaxFailedRetransmitAttempts()
        {
            throw new NotImplementedException();
        }

        // ToDo: Test Cancel Request Retransmit Attempts
        [TestMethod]
        [TestCategory("[RFC7252] Section 4.2")]
        public void TestCancelRequestRetransmitAttempts()
        {
            throw new NotImplementedException();
        }

        // ToDo: Test Reject Empty Non-Confirmable Message
        [TestMethod]
        [TestCategory("[RFC7252] Section 4.3")]
        public void TestRejectEmptyNonConfirmableMessage()
        {
            throw new NotImplementedException();
        }
    }
}
