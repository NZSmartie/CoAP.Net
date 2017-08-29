#region License
// Copyright 2017 Roman Vaughan (NZSmartie)
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;

using CoAPNet;
using CoAPNet.Tests.Mocks;

namespace CoAPNet.Tests
{
    [TestFixture]
    public class CoapClientTests
    {
        /// <summary>
        /// Timout for any Tasks
        /// </summary>
        public readonly int MaxTaskTimeout = System.Diagnostics.Debugger.IsAttached ? -1 : 2000;

        [Test]
        [Category("CoapClient")]
        public async Task TestClientRequest()
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint>(){CallBase = true};

            mockClientEndpoint
                .Setup(c => c.MockSendAsync(It.IsAny<CoapPacket>()))
                .Returns(Task.CompletedTask);

            // Act
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);
                await client.SendAsync(new CoapMessage
                {
                    Type = CoapMessageType.NonConfirmable,
                    Code = CoapMessageCode.None
                }, ct.Token);
            }

            // Assert
            mockClientEndpoint.Verify(cep => cep.SendAsync(It.IsAny<CoapPacket>()));
        }
        
        [Test]
        [Category("CoapClient")]
        public async Task TestClientResponse()
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint>{CallBase = true};

            var expected = new CoapMessage
            {
                Id = 0x1234,
                Type = CoapMessageType.Acknowledgement,
                Code = CoapMessageCode.Content,
                Options = new System.Collections.Generic.List<CoapOption>
                    {
                        new Options.ContentFormat(Options.ContentFormatType.ApplicationLinkFormat)
                    },
                Payload = Encoding.UTF8.GetBytes("</.well-known/core>")
            };

            mockClientEndpoint
                .SetupSequence(c => c.MockReceiveAsync())
                .Returns(Task.FromResult(new CoapPacket {Payload = expected.Serialise()}))
                .Throws(new CoapEndpointException("disposed"));

            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.SetNextMessageId(0x1234);
                // Sned message
                var messageId = await client.GetAsync("coap://example.com/.well-known/core", ct.Token);

                // Receive msssage
                await client.GetResponseAsync(messageId, ct.Token).ConfigureAwait(false);
            }

            // Assert
            mockClientEndpoint.Verify(x => x.ReceiveAsync(), Times.AtLeastOnce);
        }

        [Test]
        [Category("CoapClient")]
        public async Task TestClientResponseWithDelay()
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
                        new Options.ContentFormat(Options.ContentFormatType.ApplicationLinkFormat)
                    },
                Payload = Encoding.UTF8.GetBytes("</.well-known/core>")
            };

            mockClientEndpoint
                .SetupSequence(c => c.MockReceiveAsync())
                .Returns(Task.Delay(500).ContinueWith(t => new CoapPacket {Payload = expected.Serialise()}))
                .Throws(new CoapEndpointException("Endpoint closed"));

            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.RetransmitTimeout = TimeSpan.FromMilliseconds(200);
                client.MaxRetransmitAttempts = 3;
                client.SetNextMessageId(0x1234);

                // Sned message
                var messageId = await client.GetAsync("coap://example.com/.well-known/core", ct.Token);

                // Receive msssage
                await client.GetResponseAsync(messageId, ct.Token);

            }

            // Assert
            mockClientEndpoint.Verify(x => x.ReceiveAsync(), Times.AtLeastOnce);
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

            var mockClientEndpoint = new Mock<MockEndpoint> { CallBase = true };
            mockClientEndpoint
                .SetupSequence(c => c.SendAsync(It.IsAny<CoapPacket>()))
                .Returns(Task.CompletedTask);
            mockClientEndpoint
                .SetupSequence(c => c.MockReceiveAsync())
                .Returns(Task.FromResult(new CoapPacket
                {
                    Payload = new byte[] { 0x40, 0x00, 0x12, 0x34, 0xFF, 0x12, 0x34 } // "Empty" Confirmable Message with a payload
                }))
                .Returns(Task.FromResult(new CoapPacket
                {
                    Payload = new byte[] { 0x60, 0x00, 0x12, 0x34, 0xFF, 0x12, 0x34 } // "Empty" Acknowledge Message with a payload (ignored)
                }))
                .Throws(new CoapEndpointException("Endpoint closed"));

            // Act
            using (var mockClient = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                async Task receive() { await mockClient.ReceiveAsync(ct.Token); }

                Assert.ThrowsAsync<CoapMessageFormatException>(receive);
                Assert.ThrowsAsync<CoapMessageFormatException>(receive);
                Assert.ThrowsAsync<CoapEndpointException>(receive);
            }

            // Assert
            mockClientEndpoint.Verify(
                cep => cep.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expected.Serialise()))),
                Times.Exactly(1));
        }

        /* TODO: This test doesn't make sense unless there's an auto-ack involved
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public async Task TestRequestWithSeperateResponse()
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

            var mockClientEndpoint = new Mock<MockEndpoint> { CallBase = true };

            var endpoint = mockClientEndpoint.Object;

            endpoint.EnqueueReceivePacket(new CoapPacket
            {
                Payload = new CoapMessage
                {
                    Id = 0x1234,
                    Token = token,
                    Type = CoapMessageType.Acknowledgement
                }.Serialise()
            });

            // Act
            using (var mockClient = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                var messageId = await mockClient.SendAsync(requestMessage, ct.Token);

                try
                {
                    while (true)
                    {
                        var result = await mockClient.ReceiveAsync(ct.Token);
                        if (result.Message.Type == CoapMessageType.Confirmable)
                        {
                            await mockClient.SendAsync(acknowledgeMessage, ct.Token);
                            endpoint.EnqueueReceivePacket(new CoapPacket
                            {
                                Payload = new CoapMessage
                                {
                                    Id = 0xfeed,
                                    Token = token,
                                    Type = CoapMessageType.Confirmable,
                                    Code = CoapMessageCode.Content,
                                    Payload = Encoding.UTF8.GetBytes("Test Resource")
                                }.Serialise()
                            });
                        }
                    }
                }
                catch (CoapEndpointException) { }

                await mockClient.GetResponseAsync(messageId, ct.Token);

            }
            // Assert
            mockClientEndpoint.Verify(
                cep => cep.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(requestMessage.Serialise()))),
                Times.Exactly(1));

            mockClientEndpoint.Verify(
                cep => cep.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(acknowledgeMessage.Serialise()))),
                Times.Exactly(1));
        }
        */

        [Test]
        [Category("[RFC7252] Section 4.2")]
        public async Task TestRetransmissionAttempts()
        {
            // Arrange
            var requestMessage = new CoapMessage
            {
                Id = 0x1234,
                Type = CoapMessageType.Confirmable,
                Code = CoapMessageCode.Get,
                Options = new System.Collections.Generic.List<CoapOption>
                {
                    new Options.UriPath("test")
                }
            };

            var mockClientEndpoint = new Mock<MockEndpoint> { CallBase = true };
            mockClientEndpoint
                .SetupSequence(c => c.MockReceiveAsync())
                .Returns(Task.Delay(500).ContinueWith(_ => new CoapPacket
                {
                    Payload = new CoapMessage
                    {
                        Id = 0x1234,
                        Type = CoapMessageType.Acknowledgement
                    }.Serialise()
                }))
                .Throws(new CoapEndpointException("Endpoint closed"));

            // Act
            using (var mockClient = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                mockClient.RetransmitTimeout = TimeSpan.FromMilliseconds(200);
                mockClient.MaxRetransmitAttempts = 3;

                await mockClient.SendAsync(requestMessage, ct.Token);
            }

            // Assert
            mockClientEndpoint.Verify(
                cep => cep.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(requestMessage.Serialise()))),
                Times.Exactly(2));
        }

        [Test]
        [Category("[RFC7252] Section 4.2")]
        public async Task TestRetransmissionMaxAttempts()
        {
            // Arrange
            var requestMessage = new CoapMessage
            {
                Id = 0x1234,
                Type = CoapMessageType.Confirmable,
                Code = CoapMessageCode.Get,
                Options = new System.Collections.Generic.List<CoapOption>
                {
                    new Options.UriPath("test")
                }
            };

            var mockClientEndpoint = new Mock<MockEndpoint> { CallBase = true };
            mockClientEndpoint
                .SetupSequence(c => c.MockReceiveAsync())
                .Returns(Task.Delay(2000).ContinueWith<CoapPacket>(_ => throw new CoapEndpointException("Endpoint closed")));

            // Act
            using (var mockClient = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout*2);

                mockClient.RetransmitTimeout = TimeSpan.FromMilliseconds(200);
                mockClient.MaxRetransmitAttempts = 3;

                Assert.ThrowsAsync<CoapClientException>(async () => await mockClient.SendAsync(requestMessage, ct.Token));
            }

            // Assert
            mockClientEndpoint.Verify(
                cep => cep.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(requestMessage.Serialise()))),
                Times.Exactly(3));
        }

        // TODO: Test Ignore Repeated Messages
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestIgnoreRepeatedMessages()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // TODO: Test Ignore Messages Received After Timeout
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestIgnoreMessagesReceivedAfterTimeout()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // TODO: Test Ignore Non-Empty Reset Messages
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestIgnoreNonEmptyResetMessages()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // TODO: Test Ignore Acknowledgement Messages With Reserved Code
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestIgnoreAcknowledgementMessagesWithReservedCode()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // TODO: Test Reached Max Failed Retransmit Attempts
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestReachedMaxFailedRetransmitAttempts()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // TODO: Test Cancel Request Retransmit Attempts
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestCancelRequestRetransmitAttempts()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // TODO: Test Reject Empty Non-Confirmable Message
        [Test]
        [Category("[RFC7252] Section 4.3")]
        public void TestRejectEmptyNonConfirmableMessage()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // TODO: Test Multicast Message Is Marked Multicast
        [Test]
        [Category("[RFC7252] Section 8.1")]
        public async Task TestMulticastMessagFromMulticastEndpoint()
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint> { CallBase = true };

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

            mockClientEndpoint.Setup(c => c.IsMulticast).Returns(true);
            mockClientEndpoint
                .Setup(c => c.MockSendAsync(It.IsAny<CoapPacket>()))
                .Returns(Task.CompletedTask);
            mockClientEndpoint
                .SetupSequence(c => c.MockReceiveAsync())
                .Returns(Task.FromResult(new CoapPacket{Payload = expected.Serialise()}))
                .Throws(new CoapEndpointException("Endpoint closed"));


            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                ct.Token.Register(() => messageReceived.TrySetCanceled());

                try
                {
                    while (true)
                    {
                        var received = await client.ReceiveAsync(ct.Token);
                        messageReceived.TrySetResult(received.Message?.IsMulticast ?? false);
                    }
                }
                catch (CoapEndpointException)
                {
                    Debug.WriteLine($"Caught CoapEndpointException", nameof(TestMulticastMessagFromMulticastEndpoint));
                }

                await messageReceived.Task;
            }

            // Assert
            Assert.IsTrue(messageReceived.Task.IsCompleted, "Took too long to receive message");
            Assert.IsTrue(messageReceived.Task.Result, "Message is not marked as Multicast");
        }

        // TODO: Test Multicast Message is Non-Confirmable
        [Test]
        [Category("[RFC7252] Section 8.1")]
        public void TestMulticastMessageIsNonConfirmable()
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint> { CallBase = true };

            mockClientEndpoint.Setup(c => c.IsMulticast).Returns(true);
            mockClientEndpoint
                .Setup(c => c.MockSendAsync(It.IsAny<CoapPacket>()))
                .Returns(Task.CompletedTask);
            mockClientEndpoint
                .SetupSequence(c => c.MockReceiveAsync())
                .Returns(Task.FromResult(new CoapPacket
                {
                    Payload = new byte[] { 0x40, 0x00, 0x12, 0x34, 0xFF, 0x12, 0x34 } // "Empty" Confirmable Message with a payload
                }))
                .Returns(Task.FromResult(new CoapPacket
                {
                    Payload = new byte[] { 0x60, 0x00, 0x12, 0x34, 0xFF, 0x12, 0x34 } // "Empty" Acknowledge Message with a payload (ignored)
                }))
                .Throws(new CoapEndpointException("Endpoint closed"));


            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);
                async Task receive() { await client.ReceiveAsync(ct.Token); }

                Assert.ThrowsAsync<CoapMessageFormatException>(receive);
                Assert.ThrowsAsync<CoapMessageFormatException>(receive);
                Assert.ThrowsAsync<CoapEndpointException>(receive);
            }

            // Assert
            mockClientEndpoint.Verify(x => x.SendAsync(It.IsAny<CoapPacket>()), Times.Never, "Multicast Message was responded to whenn it shouldn't");
        }

        // TODO: Test Multicast Message Error Does Not Reset
        [Test]
        [Category("[RFC7252] Section 8.1")]
        public void TestMulticastMessageErrorDoesNotReset()
        {
            Assert.Inconclusive("Not Implemented");
        }
    }
}
