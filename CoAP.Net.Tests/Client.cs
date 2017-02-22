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
            var mockClientEndpoint = new Mock<ICoapEndpoint>();

            mockClientEndpoint
                .Setup(c => c.SendAsync(It.IsAny<CoapPayload>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                client.SendAsync(new CoapMessage
                {
                    Type = CoapMessageType.Confirmable,
                    Code = CoapMessageCode.None
                }).Wait();

                Mock.Verify(mockClientEndpoint);
            }
        }

        [TestMethod]
        public void TestClientResponse()
        {
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
                .Setup(c => c.ReceiveAsync())
                .Returns(Task.FromResult(mockPayload.Object))
                .Verifiable();

            using (var client = new CoapClient(mockClientEndpoint.Object))
            {
                var responseTask = client.GetAsync("coap://example.com/.well-known/core");
                responseTask.Wait();

                // Ensure ICoapTransport.ReceiveAsync was called
                Mock.Verify(mockClientEndpoint);

                // Verify the message we got back is the expected response
                Assert.AreEqual(expected.Id, responseTask.Result.Id);
                Assert.AreEqual(CoapMessageType.Acknowledgement, responseTask.Result.Type);
                Assert.AreEqual(CoapMessageCode.Content, responseTask.Result.Code);

            }
        }
    }
}
