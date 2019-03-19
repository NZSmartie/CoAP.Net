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
                .Setup(c => c.MockSendAsync(It.IsAny<CoapPacket>(), It.IsAny<CancellationToken>()))
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
            mockClientEndpoint.Verify(cep => cep.SendAsync(It.IsAny<CoapPacket>(), It.IsAny<CancellationToken>()));
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
                .SetupSequence(c => c.MockReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new CoapPacket {Payload = expected.ToBytes()}))
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
            mockClientEndpoint.Verify(x => x.ReceiveAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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
                .SetupSequence(c => c.MockReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.Delay(500).ContinueWith(t => new CoapPacket {Payload = expected.ToBytes()}))
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
            mockClientEndpoint.Verify(x => x.ReceiveAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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
                .SetupSequence(c => c.SendAsync(It.IsAny<CoapPacket>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockClientEndpoint
                .SetupSequence(c => c.MockReceiveAsync(It.IsAny<CancellationToken>()))
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
                cep => cep.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expected.ToBytes())), It.IsAny<CancellationToken>()),
                Times.Exactly(1));
        }

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
                .SetupSequence(c => c.MockReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.Delay(500).ContinueWith(_ => new CoapPacket
                {
                    Payload = new CoapMessage
                    {
                        Id = 0x1234,
                        Type = CoapMessageType.Acknowledgement
                    }.ToBytes()
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
                cep => cep.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(requestMessage.ToBytes())), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestRetransmissionMaxAttempts()
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
                .SetupSequence(c => c.MockReceiveAsync(It.IsAny<CancellationToken>()))
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
                cep => cep.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(requestMessage.ToBytes())), It.IsAny<CancellationToken>()),
                Times.Exactly(3));
        }

        [Test]
        [Category("[RFC7252] Section 4.2")]
        public async Task TestIgnoreRepeatedMessages()
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
                .SetupSequence(c => c.MockReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new CoapPacket { Payload = expected.ToBytes() }))
                .Returns(Task.FromResult(new CoapPacket { Payload = expected.ToBytes() }))
                .Returns(Task.FromResult(new CoapPacket { Payload = expected.ToBytes() }))
                .Throws(new CoapEndpointException("disposed"));

            int receiveCount = 0;

            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                client.SetNextMessageId(0x1234);
                // Sned message
                var messageId = await client.GetAsync("coap://example.com/.well-known/core", ct.Token);

                try
                {
                    while (true)
                    {
                        await client.ReceiveAsync(ct.Token);
                        receiveCount++;
                    }
                }
                catch (CoapEndpointException)
                {
                    Debug.WriteLine($"Caught CoapEndpointException", nameof(TestReceiveMulticastMessagFromMulticastEndpoint));
                }
            }

            // Assert
            Assert.AreEqual(1, receiveCount, "Did not receive same message exactly once");
        }

        [Test]
        [Category("[RFC7252] Section 4.2")]
        public async Task TestRepeatedMessagesAfterExpirey()
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
                .SetupSequence(c => c.MockReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new CoapPacket { Payload = expected.ToBytes() })) // Received
                .Returns(Task.FromResult(new CoapPacket { Payload = expected.ToBytes() })) // Ignored (considered duplicate)
                .Returns(Task.Delay(500).ContinueWith(_ => new CoapPacket { Payload = expected.ToBytes() })) // Received after expirey
                .Throws(new CoapEndpointException("disposed"));

            int receiveCount = 0;

            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);
                client.MessageCacheTimeSpan = TimeSpan.FromMilliseconds(250);
                client.SetNextMessageId(0x1234);
                // Sned message
                var messageId = await client.GetAsync("coap://example.com/.well-known/core", ct.Token);

                try
                {
                    while (true)
                    {
                        await client.ReceiveAsync(ct.Token);
                        receiveCount++;
                    }
                }
                catch (CoapEndpointException)
                {
                    Debug.WriteLine($"Caught CoapEndpointException", nameof(TestReceiveMulticastMessagFromMulticastEndpoint));
                }
            }

            // Assert
            Assert.AreEqual(2, receiveCount, "Did not receive same message exactly twice");
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
        public async Task TestIgnoreNonEmptyResetMessages()
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint> { CallBase = true };

            var nonEmptyReset = new CoapMessage
            {
                Code = CoapMessageCode.Get,
                Type = CoapMessageType.Reset,
                Options =
                {
                    new Options.ContentFormat(Options.ContentFormatType.TextPlain)
                },
                Payload = Encoding.UTF8.GetBytes("hello")
            };

            mockClientEndpoint
                .SetupSequence(c => c.MockReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new CoapPacket { Payload = nonEmptyReset.ToBytes() })) // Received
                .Throws(new CoapEndpointException("disposed"));

            int receiveCount = 0;

            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);
                
                try
                {
                    while (true)
                    {
                        await client.ReceiveAsync(ct.Token);
                        receiveCount++;
                    }
                }
                catch (CoapEndpointException)
                {
                    Debug.WriteLine($"Caught CoapEndpointException", nameof(TestReceiveMulticastMessagFromMulticastEndpoint));
                }
            }

            // Assert
            Assert.AreEqual(0, receiveCount, "Should not receive anyhting");
        }

        // TODO: Test Ignore Acknowledgement Messages With Reserved Code
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestIgnoreAcknowledgementMessagesWithReservedCode()
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint> { CallBase = true };

            var expected = new CoapMessage
            {
                Id = 0x1234,
                Type = CoapMessageType.Acknowledgement,
                Code = new CoapMessageCode(1, 0),
                Options = new System.Collections.Generic.List<CoapOption>
                    {
                        new Options.ContentFormat(Options.ContentFormatType.ApplicationLinkFormat)
                    },
                Payload = Encoding.UTF8.GetBytes("</.well-known/core>")
            };

            mockClientEndpoint
                .SetupSequence(c => c.MockReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new CoapPacket { Payload = expected.ToBytes() }))
                .Returns(Task.Delay(2000).ContinueWith<CoapPacket>(_ => throw new CoapEndpointException("Endpoint closed")));

            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);
                client.RetransmitTimeout = TimeSpan.FromMilliseconds(200);
                client.MaxRetransmitAttempts = 3;
                client.SetNextMessageId(0x1234);

                // Assert
                Assert.ThrowsAsync<CoapClientException>(async () => await client.GetAsync("coap://example.com/.well-known/core", ct.Token));
            }
        }

        // TODO: Test Cancel Request Retransmit Attempts
        [Test]
        [Category("[RFC7252] Section 4.2")]
        public void TestCancelRequestRetransmitAttempts()
        {
            Assert.Inconclusive("Not Implemented");
        }

        // TODO: Test Reject Empty Confirmable Message
        [Test]
        [Category("[RFC7252] Section 4.3")]
        public async Task TestRejectEmptyConfirmableMessage()
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint> { CallBase = true };

            var ping = new CoapMessage
            {
                Id = 0x1234,
                Type = CoapMessageType.Confirmable,
            };

            var expected = new CoapMessage
            {
                Id = 0x1234,
                Type = CoapMessageType.Reset,
            };

            mockClientEndpoint
                .SetupSequence(c => c.MockReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new CoapPacket { Payload = ping.ToBytes() }))
                .Throws(new CoapEndpointException("disposed"));

            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                try
                {
                    while (true)
                    {
                        await client.ReceiveAsync(ct.Token);
                    }
                }
                catch (CoapEndpointException)
                {
                    Debug.WriteLine($"Caught CoapEndpointException", nameof(TestReceiveMulticastMessagFromMulticastEndpoint));
                }
            }

            // Assert
            mockClientEndpoint.Verify(x => x.ReceiveAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            mockClientEndpoint.Verify(x => x.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expected.ToBytes())), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        // TODO: Test Multicast Message Is Marked Multicast
        [Test]
        [Category("[RFC7252] Section 8.1")]
        public async Task TestReceiveMulticastMessagFromMulticastEndpoint()
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
                .Setup(c => c.MockSendAsync(It.IsAny<CoapPacket>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockClientEndpoint
                .SetupSequence(c => c.MockReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new CoapPacket{Payload = expected.ToBytes()}))
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
                    Debug.WriteLine($"Caught CoapEndpointException", nameof(TestReceiveMulticastMessagFromMulticastEndpoint));
                }

                await messageReceived.Task;
            }

            // Assert
            Assert.IsTrue(messageReceived.Task.IsCompleted, "Took too long to receive message");
            Assert.IsTrue(messageReceived.Task.Result, "Message is not marked as Multicast");
        }

        [Test]
        [Category("[RFC7252] Section 8.1")]
        public async Task TestSendMulticastMessagToMulticastEndpoint()
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint> { CallBase = true };
            mockClientEndpoint
                // Ensure a multicast placeholder endpoint is used.
                .Setup(e => e.SendAsync(It.Is<CoapPacket>(p => p.Endpoint is CoapEndpoint && p.Endpoint.IsMulticast == true), It.IsAny<CancellationToken>()))
                .CallBase()
                .Verifiable("Message was not sent via multicast endpoint");

            var message = new CoapMessage
            {
                IsMulticast = true,
                Type = CoapMessageType.NonConfirmable,
                Code = CoapMessageCode.Get,
                Options = new System.Collections.Generic.List<CoapOption>
                    {
                        new Options.ContentFormat(Options.ContentFormatType.ApplicationLinkFormat)
                    },
                Payload = Encoding.UTF8.GetBytes("</.well-known/core>")
            };


            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                await client.SendAsync(message, ct.Token);
            }

            // Assert
            Mock.Verify(mockClientEndpoint);
        }

        [Test]
        [Category("[RFC7252] Section 8.1")]
        public async Task TestSendMulticastMessagToExplicitMulticastEndpoint()
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint> { CallBase = true };
            mockClientEndpoint
                .Setup(e => e.SendAsync(It.IsAny<CoapPacket>(), It.IsAny<CancellationToken>()))
                .CallBase()
                .Verifiable("Message was not sent via multicast endpoint");

            var destEndpoint = new CoapEndpoint { IsMulticast = true };

            var message = new CoapMessage
            {
                IsMulticast = true,
                Type = CoapMessageType.NonConfirmable,
                Code = CoapMessageCode.Get,
                Options = new System.Collections.Generic.List<CoapOption>
                    {
                        new Options.ContentFormat(Options.ContentFormatType.ApplicationLinkFormat)
                    },
                Payload = Encoding.UTF8.GetBytes("</.well-known/core>")
            };


            // Ack
            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var ct = new CancellationTokenSource(MaxTaskTimeout);

                await client.SendAsync(message, destEndpoint, ct.Token);
            }

            // Assert
            Mock.Verify(mockClientEndpoint);
        }

        [Test]
        [Category("[RFC7252] Section 8.1")]
        public void TestSendConfirmableMulticastMessagThrowsCoapClientException()
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint> { CallBase = true };

            var destEndpoint = new CoapEndpoint { IsMulticast = true };

            var message = new CoapMessage
            {
                IsMulticast = true,
                Type = CoapMessageType.Confirmable,
                Code = CoapMessageCode.Get,
                Options = new System.Collections.Generic.List<CoapOption>
                    {
                        new Options.ContentFormat(Options.ContentFormatType.ApplicationLinkFormat)
                    },
                Payload = Encoding.UTF8.GetBytes("</.well-known/core>")
            };
            
            // Ack
            AsyncTestDelegate action = async () => {
                using (var client = new CoapClient(mockClientEndpoint.Object))
                {
                    var ct = new CancellationTokenSource(MaxTaskTimeout);

                    await client.SendAsync(message, destEndpoint, ct.Token);
                }
            };

            // Assert
            Assert.ThrowsAsync<CoapClientException>(action);
        }

        [Test]
        [Category("[RFC7252] Section 8.1")]
        public void TestSendMulticastMessagtoNonMulticastEndpointThrowsCoapClientException()
        {
            // Arrange
            var mockClientEndpoint = new Mock<MockEndpoint> { CallBase = true };

            var destEndpoint = new CoapEndpoint { IsMulticast = false };

            var message = new CoapMessage
            {
                IsMulticast = true,
                Type = CoapMessageType.NonConfirmable,
                Code = CoapMessageCode.Get,
                Options = new System.Collections.Generic.List<CoapOption>
                    {
                        new Options.ContentFormat(Options.ContentFormatType.ApplicationLinkFormat)
                    },
                Payload = Encoding.UTF8.GetBytes("</.well-known/core>")
            };

            // Ack
            AsyncTestDelegate action = async () => {
                using (var client = new CoapClient(mockClientEndpoint.Object))
                {
                    var ct = new CancellationTokenSource(MaxTaskTimeout);

                    await client.SendAsync(message, destEndpoint, ct.Token);
                }
            };

            // Assert
            Assert.ThrowsAsync<CoapClientException>(action);
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
                .Setup(c => c.MockSendAsync(It.IsAny<CoapPacket>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockClientEndpoint
                .SetupSequence(c => c.MockReceiveAsync(It.IsAny<CancellationToken>()))
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
            mockClientEndpoint.Verify(x => x.SendAsync(It.IsAny<CoapPacket>(), It.IsAny<CancellationToken>()), Times.Never, "Multicast Message was responded to whenn it shouldn't");
        }

        // TODO: Test Multicast Message Error Does Not Reset
        [Test]
        [Category("[RFC7252] Section 8.1")]
        public void TestMulticastMessageErrorDoesNotReset()
        {
            Assert.Inconclusive("Not Implemented");
        }

        [Test]
        public void DisposeCoapClient_With_NonDisposableEndpoint()
        {
            // Arrange
            var endpoint = new NonDisposableEndpoint();
            var client = new CoapClient(endpoint);

            Task receiveTask;
            var ct = new CancellationTokenSource(MaxTaskTimeout);

            // Ack
            receiveTask = client.ReceiveAsync(ct.Token);
            client.Dispose();

            // Assert
            Assert.ThrowsAsync<CoapEndpointException>(async () => await receiveTask, $"{nameof(CoapClient.ReceiveAsync)} did not throw an {nameof(CoapEndpointException)} when {nameof(CoapClient)} was disposed.");
            Assert.That(ct.IsCancellationRequested, Is.False, "The test's safety CancellationToken timed out");
        }

        [Test]
        public void CancelReceiveAsync()
        {
            // Arrange
            var endpoint = new NonDisposableEndpoint();

            var safetyCt = new CancellationTokenSource(MaxTaskTimeout);
            var testCt = new CancellationTokenSource(MaxTaskTimeout / 2);
            Task receiveTask1;
            Task receiveTask2;
            Task receiveTask3;

            // Ack
            using (var client = new CoapClient(endpoint))
            {
                receiveTask1 = client.ReceiveAsync(testCt.Token);
                receiveTask2 = client.ReceiveAsync(testCt.Token);
                receiveTask3 = client.ReceiveAsync(testCt.Token);

                Task.Run(() =>
                {
                    // Assert
                    Assert.ThrowsAsync<TaskCanceledException>(
                        async () => await receiveTask1, $"{nameof(CoapClient.ReceiveAsync)} did not throw an {nameof(CoapEndpointException)} when {nameof(CoapClient)} was disposed.");
                    Assert.ThrowsAsync<TaskCanceledException>(
                        async () => await receiveTask2, $"{nameof(CoapClient.ReceiveAsync)} did not throw an {nameof(CoapEndpointException)} when {nameof(CoapClient)} was disposed.");
                    Assert.ThrowsAsync<TaskCanceledException>(
                        async () => await receiveTask3, $"{nameof(CoapClient.ReceiveAsync)} did not throw an {nameof(CoapEndpointException)} when {nameof(CoapClient)} was disposed.");
                }, safetyCt.Token).Wait();
            }

            Assert.That(testCt.IsCancellationRequested, Is.True, "The test's CancellationToken should have timed out.");
            Assert.That(safetyCt.IsCancellationRequested, Is.False, "The test's safety CancellationToken timed out");
        }
    }
}
