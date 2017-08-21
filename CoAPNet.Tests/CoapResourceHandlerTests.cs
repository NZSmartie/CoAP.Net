using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

using CoAPNet.Utils;

namespace CoAPNet.Tests
{
    [TestFixture]
    public class CoapResourceHandlerTests
    {
        private Mock<ICoapEndpoint> _endpoint;
        private Uri _baseUri;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _baseUri = new Uri("coap://localhost/");
        }

        [SetUp]
        public void Setup()
        {
            _endpoint = new Mock<ICoapEndpoint>();
            _endpoint.Setup(e => e.BaseUri).Returns(_baseUri);
            _endpoint.Setup(e => e.SendAsync(It.IsAny<CoapPacket>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));
        }

        [Test]
        public void TestResponse()
        {
            // Arrange
            _endpoint
                .Setup(c => c.SendAsync(It.IsAny<CoapPacket>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(0))
                .Verifiable();

            var mockResource = new Mock<CoapResource>("/test");
            mockResource.Setup(r => r.Get()).Returns(new CoapMessage());

            var request = new CoapMessage { Code = CoapMessageCode.Get };
            request.FromUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();
            service.Resources.Add(mockResource.Object);

            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.Serialise()).Wait();
            //_client.Raise(c => c.OnMessageReceived += null, new CoapMessageReceivedEventArgs {Message = request});
            
            // Assert
            Mock.Verify(_endpoint, mockResource);
        }

        [Test]
        public void TestGetRequest()
        {
            // Arrange
            var mockResource = new Mock<CoapResource>("/test");
            mockResource
                .Setup(r => r.Get())
                .Returns(new CoapMessage())
                .Verifiable();

            var request = new CoapMessage { Code = CoapMessageCode.Get };
            request.FromUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();

            service.Resources.Add(mockResource.Object);
            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.Serialise()).Wait();
            
            // Assert
            Mock.Verify(_endpoint, mockResource);
        }

        [Test]
        public void TestPostRequest()
        {
            // Arrange
            var mockResource = new Mock<CoapResource>("/test");
            mockResource
                .Setup(r => r.Post())
                .Returns(new CoapMessage())
                .Verifiable();

            var request = new CoapMessage { Code = CoapMessageCode.Post };
            request.FromUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();

            service.Resources.Add(mockResource.Object);
            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.Serialise()).Wait();

            // Assert
            Mock.Verify(_endpoint, mockResource);
        }

        [Test]
        public void TestPutRequest()
        {
            // Arrange
            var mockResource = new Mock<CoapResource>("/test");
            mockResource
                .Setup(r => r.Put())
                .Returns(new CoapMessage())
                .Verifiable();

            var request = new CoapMessage { Code = CoapMessageCode.Put };
            request.FromUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();

            service.Resources.Add(mockResource.Object);
            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.Serialise()).Wait();

            // Assert
            Mock.Verify(_endpoint, mockResource);
        }

        [Test]
        public void TestDeleteRequest()
        {
            // Arrange
            var mockResource = new Mock<CoapResource>("/test");
            mockResource
                .Setup(r => r.Delete())
                .Returns(new CoapMessage())
                .Verifiable();

            var request = new CoapMessage { Code = CoapMessageCode.Delete };
            request.FromUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();

            service.Resources.Add(mockResource.Object);
            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.Serialise()).Wait();

            // Assert
            Mock.Verify(_endpoint, mockResource);
        }

        [Test]
        public void TestResourceMethodNotImplemented()
        {
            // Arrange
            var expectedMessage = CoapMessageUtility
                .CreateMessage(CoapMessageCode.NotImplemented, new NotImplementedException().Message).Serialise();
            _endpoint
                .Setup(c => c.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expectedMessage)), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(0))
                .Verifiable();

            var request = new CoapMessage { Code = CoapMessageCode.Get };
            request.FromUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();

            service.Resources.Add(new CoapResource("/test"));
            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.Serialise()).Wait();

            // Assert
            Mock.Verify(_endpoint);
        }

        [Test]
        public void TestResourceNotFound()
        {
            // Arrange
            var expectedMessage = CoapMessageUtility.CreateMessage(CoapMessageCode.NotFound, $"Resouce {new Uri(_baseUri, "/test")} was not found").Serialise();
            _endpoint
                .Setup(e => e.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expectedMessage)), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(0))
                .Verifiable();

            var request = new CoapMessage { Code = CoapMessageCode.Get };
            request.FromUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();

            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.Serialise()).Wait();

            // Assert
            Mock.Verify(_endpoint);
        }
    }

    public class MockConnectionInformation : ICoapConnectionInformation
    {
        public MockConnectionInformation(ICoapEndpoint endPoint)
        {
            LocalEndpoint = endPoint;
        }

        public ICoapEndpoint LocalEndpoint { get; }

        public ICoapEndpoint RemoteEndpoint => new Mock<ICoapEndpoint>().Object;
    }
}