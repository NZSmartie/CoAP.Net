using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit;
using NUnit.Framework;
using Moq;

using CoAPNet;
using NUnit.Framework.Internal;

namespace CoAPNet.Tests
{
    [TestFixture]
    public class Client
    {

        /// <summary>
        /// Timout for any Tasks
        /// </summary>
        public readonly int MaxTaskTimeout = System.Diagnostics.Debugger.IsAttached ? -1 : 2000;

        [Test]
        [Category("CoapClient")]
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
        
        [Test]
        [Category("CoapClient")]
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
                .Throws(new CoapEndpointException("Endpoint closed"));

            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                // Sned message
                var sendTask = client.GetAsync("coap://example.com/.well-known/core");
                sendTask.Wait(MaxTaskTimeout);

                if (!sendTask.IsCompleted)
                    Assert.Fail("sendTask took too long to complete");

                client.Listen(); // enable loop back thingy

                // Receive msssage
                var responseTask = client.GetResponseAsync(sendTask.Result);
                responseTask.Wait(MaxTaskTimeout);

                if (!responseTask.IsCompleted)
                    Assert.Fail("responseTask took too long to complete");

            }

            // Assert
            mockClientEndpoint.Verify(x => x.ReceiveAsync(), Times.AtLeastOnce);
        }

        [Test]
        [Category("CoapClient")]
        public void TestClientOnMessageReceivedEvent()
        {
            // Arrange
            var expected = new CoapMessage
            {
                Id = 0x1234,
                Type = CoapMessageType.Acknowledgement,
                Code = CoapMessageCode.None,
            };
            var mockClientEndpoint = new Mock<ICoapEndpoint>();
            var clientOnMessageReceivedEventCalled = false;

            mockClientEndpoint
                .SetupSequence(c => c.ReceiveAsync())
                .Returns(Task.FromResult(new CoapPayload
                {
                    Payload = expected.Serialise()
                }))
                .Throws(new CoapEndpointException("Endpoint closed"));

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
            }

            // Assert
            Assert.IsTrue(clientOnMessageReceivedEventCalled);
        }

        [Test]
        [Category("[RFC7252] Section 4.1")]
        public void TestRejectEmptyMessageWithFormatError()
        {
            // Arrange
            var expected = new CoapMessage
            {
                Id = 0x1234,
                Type = CoapMessageType.Reset,
                Code = CoapMessageCode.None,
            };

            var mockClientEndpoint = new Mock<ICoapEndpoint>();
            mockClientEndpoint
                .SetupSequence(c => c.ReceiveAsync())
                .Returns(Task.FromResult(new CoapPayload
                {
                    Payload = new byte[] { 0x40, 0x00, 0x12, 0x34, 0xFF, 0x12, 0x34 } // "Empty" Confirmable Message with a payload
                }))
                .Returns(Task.FromResult(new CoapPayload
                {
                    Payload = new byte[] { 0x60, 0x00, 0x12, 0x34, 0xFF, 0x12, 0x34 } // "Empty" Acknowledge Message with a payload (ignored)
                }))
                .Throws(new CoapEndpointException("Endpoint closed"));

            // Act
            using (var mockClient = new CoapClient(mockClientEndpoint.Object))
            {
                mockClient.Listen();

                // Assert
                mockClientEndpoint.Verify(
                    cep => cep.SendAsync(It.Is<CoapPayload>(p => p.Payload.SequenceEqual(expected.Serialise()))),
                    Times.Exactly(1));
            }
        }

        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestRequestWithSeperateResponse()
        {
            // Arrange
            var token = new byte[] { 0xC0, 0xFF, 0xEE };
            var requestMessage = new CoapMessage
            {
                Id = 0x1234,
                Token = token,
                Type = CoapMessageType.Confirmable,
                Code = CoapMessageCode.Get,
                Options = new System.Collections.Generic.List<CoapOption>
                {
                    new Options.UriPath("test")
                }
            };

            var acknowledgeMessage = new CoapMessage
            {
                Id = 0xfeed,
                Type = CoapMessageType.Acknowledgement,
                Token = token
            };

            var mockClientEndpoint = new Mock<ICoapEndpoint>();
            mockClientEndpoint
                .SetupSequence(c => c.ReceiveAsync())
                .Returns(Task.FromResult(new CoapPayload
                {
                    Payload = new CoapMessage
                    {
                        Id = 0x1234,
                        Token = token,
                        Type = CoapMessageType.Acknowledgement
                    }.Serialise()
                }))
                .Returns(Task.FromResult(new CoapPayload
                {
                    Payload = new CoapMessage
                    {
                        Id = 0xfeed,
                        Token = token,
                        Type = CoapMessageType.Confirmable,
                        Code = CoapMessageCode.Content,
                        Payload = Encoding.UTF8.GetBytes("Test Resource")
                    }.Serialise()
                }))
                .Throws(new CoapEndpointException("Endpoint closed"));

            // Act
            using (var mockClient = new CoapClient(mockClientEndpoint.Object))
            {
                mockClient.OnMessageReceived += (s, e) =>
                {
                    mockClient.SendAsync(acknowledgeMessage).Wait(MaxTaskTimeout);
                };

                var requestTask = mockClient.SendAsync(requestMessage);
                requestTask.Wait(MaxTaskTimeout);
                if (!requestTask.IsCompleted)
                    Assert.Fail("Took too long to send Get request");


                mockClient.Listen();

                var reponseTask = mockClient.GetResponseAsync(requestTask.Result);
                reponseTask.Wait(MaxTaskTimeout);
                if (!reponseTask.IsCompleted)
                    Assert.Fail("Took too long to get reponse");

                // Assert
                mockClientEndpoint.Verify(
                    cep => cep.SendAsync(It.Is<CoapPayload>(p => p.Payload.SequenceEqual(requestMessage.Serialise()))),
                    Times.Exactly(1));

                mockClientEndpoint.Verify(
                    cep => cep.SendAsync(It.Is<CoapPayload>(p => p.Payload.SequenceEqual(acknowledgeMessage.Serialise()))),
                    Times.Exactly(1));
            }
        }

        // ToDo: Test Retransmit Delays
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestRetransmitDelays()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // ToDo: Test Ignore Repeated Messages
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestIgnoreRepeatedMessages()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // ToDo: Test Ignore Messages Received After Timeout
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestIgnoreMessagesReceivedAfterTimeout()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // ToDo: Test Ignore Non-Empty Reset Messages
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestIgnoreNonEmptyResetMessages()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // ToDo: Test Ignore Acknowledgement Messages With Reserved Code
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestIgnoreAcknowledgementMessagesWithReservedCode()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // Todo: Test Reached Max Failed Retransmit Attempts
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestReachedMaxFailedRetransmitAttempts()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // ToDo: Test Cancel Request Retransmit Attempts
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestCancelRequestRetransmitAttempts()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // ToDo: Test Reject Empty Non-Confirmable Message
        [Test]
        [Category("[RFC7252] Section 4.3")]
        public void TestRejectEmptyNonConfirmableMessage()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // ToDo: Test Multicast Message Is Marked Multicast
        [Test]
        [Category("[RFC7252] Section 8.1")]
        public void TestMulticastMessagFromMulticastEndpoint()
        {
            // Arrange
            var mockClientEndpoint = new Mock<ICoapEndpoint>();
            var mockPayload = new Mock<CoapPayload>();

            var messageReceived = new TaskCompletionSource<bool>();

            var expected = new CoapMessage
            {
                Type = CoapMessageType.NonConfirmable,
                Code = CoapMessageCode.Get,
                Options = new System.Collections.Generic.List<CoapOption>
                    {
                        new Options.ContentFormat(Options.ContentFormatType.ApplicationLinkFormat)
                    },
                Payload = Encoding.UTF8.GetBytes("</.well-known/core>")
            };

            mockPayload
                .Setup(p => p.Payload)
                .Returns(() => expected.Serialise());

            mockClientEndpoint.Setup(c => c.IsMulticast).Returns(true);
            mockClientEndpoint
                .Setup(c => c.SendAsync(It.IsAny<CoapPayload>()))
                .Returns(Task.CompletedTask);
            mockClientEndpoint
                .SetupSequence(c => c.ReceiveAsync())
                .Returns(Task.FromResult(mockPayload.Object))
                .Throws(new CoapEndpointException("Endpoint closed"));


            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                client.OnMessageReceived += (s, e) => messageReceived.SetResult(e?.Message?.IsMulticast ?? false);
                client.Listen(); // enable loop back thingy

                messageReceived.Task.Wait(MaxTaskTimeout);
            }

            // Assert
            Assert.IsTrue(messageReceived.Task.IsCompleted, "Took too long to receive message");
            Assert.IsTrue(messageReceived.Task.Result, "Message is not marked as Multicast");
        }

        // ToDo: Test Multicast Message is Non-Confirmable
        [Test]
        [Category("[RFC7252] Section 8.1")]
        public void TestMulticastMessageIsNonConfirmable()
        {
            // Arrange
            var mockClientEndpoint = new Mock<ICoapEndpoint>();
            var closedEventSource = new TaskCompletionSource<bool>();

            mockClientEndpoint.Setup(c => c.IsMulticast).Returns(true);
            mockClientEndpoint
                .Setup(c => c.SendAsync(It.IsAny<CoapPayload>()))
                .Returns(Task.CompletedTask);
            mockClientEndpoint
                .SetupSequence(c => c.ReceiveAsync())
                .Returns(Task.FromResult(new CoapPayload
                {
                    Payload = new byte[] { 0x40, 0x00, 0x12, 0x34, 0xFF, 0x12, 0x34 } // "Empty" Confirmable Message with a payload
                }))
                .Returns(Task.FromResult(new CoapPayload
                {
                    Payload = new byte[] { 0x60, 0x00, 0x12, 0x34, 0xFF, 0x12, 0x34 } // "Empty" Acknowledge Message with a payload (ignored)
                }))
                .Throws(new CoapEndpointException("Endpoint closed"));


            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                client.OnClosed += (s, e) => closedEventSource.SetResult(true);
                client.Listen(); // enable loop back thingy

                closedEventSource.Task.Wait(MaxTaskTimeout);
            }

            // Assert
            Assert.IsTrue(closedEventSource.Task.IsCompleted, "Took too long to receive message");
            mockClientEndpoint.Verify(x => x.SendAsync(It.IsAny<CoapPayload>()), Times.Never, "Multicast Message was responded to whenn it shouldn't");
        }

        // ToDo: Test Multicast Message Error Does Not Reset
        [Test]
        [Category("[RFC7252] Section 8.1")]
        public void TestMulticastMessageErrorDoesNotReset()
        {
            Assert.Inconclusive("Not Implemented");
        }
    }
}
