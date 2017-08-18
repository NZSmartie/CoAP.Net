using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace CoAPNet.Tests
{
    [TestFixture]
    public class CoapServiceTests
    {
        private Mock<ICoapEndpoint> _endpoint;
        private Mock<CoapClient> _client;
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

            _client = new Mock<CoapClient>(_endpoint.Object);
            _client.Setup(c => c.SendAsync(It.IsAny<CoapMessage>(), null)).Returns(Task.FromResult(0));
        }

        [Test]
        public void TestResponse()
        {
            // Arrange
            _client
                .Setup(c => c.SendAsync(It.IsAny<CoapMessage>(), null))
                .Returns(Task.FromResult(0))
                .Verifiable();

            var mockResource = new Mock<CoapResource>("/test");
            mockResource.Setup(r => r.Get()).Returns(new CoapMessage());

            var request = new CoapMessage { Code = CoapMessageCode.Get };
            request.FromUri(new Uri(_baseUri, "/test"));

            // Act
            using (var service = new CoapService(_client.Object))
            {
                service.Resources.Add(mockResource.Object);

                _client.Raise(c => c.OnMessageReceived += null, new CoapMessageReceivedEventArgs {Message = request});
            }
            
            // Assert
            Mock.Verify(_client, mockResource);
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

            var request = new CoapMessage {Code = CoapMessageCode.Get};
            request.FromUri(new Uri(_baseUri, "/test"));

            // Act
            using (var service = new CoapService(_client.Object))
            {
                service.Resources.Add(mockResource.Object);

                _client.Raise(c => c.OnMessageReceived += null, new CoapMessageReceivedEventArgs {Message = request});
            }
            
            // Assert
            Mock.Verify(_client, mockResource);
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
            using (var service = new CoapService(_client.Object))
            {
                service.Resources.Add(mockResource.Object);

                _client.Raise(c => c.OnMessageReceived += null, new CoapMessageReceivedEventArgs {Message = request});

            }
            
            // Assert
            Mock.Verify(_client, mockResource);
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
            using (var service = new CoapService(_client.Object))
            {
                service.Resources.Add(mockResource.Object);

                _client.Raise(c => c.OnMessageReceived += null, new CoapMessageReceivedEventArgs {Message = request});

            }

            // Assert
            Mock.Verify(_client, mockResource);
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
            using (var service = new CoapService(_client.Object))
            {
                service.Resources.Add(mockResource.Object);

                _client.Raise(c => c.OnMessageReceived += null, new CoapMessageReceivedEventArgs {Message = request});
            }

            // Assert
            Mock.Verify(_client, mockResource);
        }

        [Test]
        public void TestResourceMethodNotImplemented()
        {
            // Arrange
            _client
                .Setup(c => c.SendAsync(It.Is<CoapMessage>(m => m.Code == CoapMessageCode.NotImplemented), null))
                .Returns(Task.FromResult(0))
                .Verifiable();

            var request = new CoapMessage {Code = CoapMessageCode.Get};
            request.FromUri(new Uri(_baseUri, "/test"));

            // Act
            using (var service = new CoapService(_client.Object))
            {
                service.Resources.Add(new CoapResource("/test"));

                _client.Raise(c => c.OnMessageReceived += null,
                    new CoapMessageReceivedEventArgs {Message = request});

            }

            // Assert
            Mock.Verify(_client);
        }

        [Test]
        public void TestResourceNotFound()
        {
            // Arrange
            _client
                .Setup(c => c.SendAsync(It.Is<CoapMessage>(m => m.Code == CoapMessageCode.NotFound), null))
                .Returns(Task.FromResult(0))
                .Verifiable();

            var request = new CoapMessage { Code = CoapMessageCode.Get };
            request.FromUri(new Uri(_baseUri, "/test"));

            // Act
            using (var service = new CoapService(_client.Object))
            {
                _client.Raise(c => c.OnMessageReceived += null, new CoapMessageReceivedEventArgs {Message = request});

            }

            // Assert
            Mock.Verify(_client);
        }
    }
}