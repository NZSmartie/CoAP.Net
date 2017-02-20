using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using CoAP.Net;

namespace CoAP.Net.Tests
{
    [TestClass]
    public class Client
    {
        [TestMethod]
        public void TestClientRequest()
        {
            var clientTransport = new Mock<ICoapTransport>();
            var endpoint = new Mock<ICoapEndpoint>();

            clientTransport
                .Setup(c => c.SendAsync(It.IsAny<ICoapPayload>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            using (var client = new CoapClient(endpoint.Object, clientTransport.Object))
            {
                client.SendAsync(new CoapMessage
                {
                    Type = CoapMessageType.Confirmable,
                    Code = CoapMessageCode.None
                }).Wait();

                Mock.Verify(clientTransport);
            }
        }

        [TestMethod]
        public void TestClientResponse()
        {
            var mockClientTransport = new Mock<ICoapTransport>();
            var mockEndpoint = new Mock<ICoapEndpoint>();
            var mockPayload = new Mock<ICoapPayload>();

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
            mockClientTransport
                .Setup(c => c.SendAsync(It.IsAny<ICoapPayload>()))
                // Copy the ID from the message sent out, to the message for the client to receive
                .Callback<ICoapPayload>((p) => expected.Id = p.MessageId)
                .Returns(Task.CompletedTask);
            mockClientTransport
                .Setup(c => c.ReceiveAsync())
                .Returns(Task.FromResult(mockPayload.Object))
                .Verifiable();

            using (var client = new CoapClient(mockEndpoint.Object, mockClientTransport.Object))
            {
                var responseTask = client.GetAsync("coap://example.com/.well-known/core");
                responseTask.Wait();

                // Ensure ICoapTransport.ReceiveAsync was called
                Mock.Verify(mockClientTransport);

                // Verify the message we got back is the expected response
                Assert.AreEqual(expected.Id, responseTask.Result.Id);
                Assert.AreEqual(CoapMessageType.Acknowledgement, responseTask.Result.Type);
                Assert.AreEqual(CoapMessageCode.Content, responseTask.Result.Code);

            }
        }
    }
}
