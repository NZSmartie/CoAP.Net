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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

using CoAPNet.Server;
using CoAPNet.Tests.Mocks;
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

            var mockResource = new Mock<CoapResource>("/test") { CallBase = true };
            mockResource.Setup(r => r.Get(It.IsAny<CoapMessage>())).Returns(new CoapMessage());

            var request = new CoapMessage { Code = CoapMessageCode.Get };
            request.SetUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();
            service.Resources.Add(mockResource.Object);

            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.ToBytes()).Wait();
            //_client.Raise(c => c.OnMessageReceived += null, new CoapMessageReceivedEventArgs {Message = request});

            // Assert
            Mock.Verify(_endpoint, mockResource);
        }
        
        [Test]
        public void TestGetRequest()
        {
            // Arrange
            var mockResource = new Mock<CoapResource>("/test") { CallBase = true };
            mockResource
                .Setup(r => r.Get(It.IsAny<CoapMessage>()))
                .Returns(new CoapMessage())
                .Verifiable();

            var request = new CoapMessage { Code = CoapMessageCode.Get };
            request.SetUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();

            service.Resources.Add(mockResource.Object);
            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.ToBytes()).Wait();
            
            // Assert
            Mock.Verify(_endpoint, mockResource);
        }

        [Test]
        public void TestPostRequest()
        {
            // Arrange
            var mockResource = new Mock<CoapResource>("/test") { CallBase = true };
            mockResource
                .Setup(r => r.Post(It.IsAny<CoapMessage>()))
                .Returns(new CoapMessage())
                .Verifiable();

            var request = new CoapMessage { Code = CoapMessageCode.Post };
            request.SetUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();

            service.Resources.Add(mockResource.Object);
            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.ToBytes()).Wait();

            // Assert
            Mock.Verify(_endpoint, mockResource);
        }

        [Test]
        public void TestPutRequest()
        {
            // Arrange
            var mockResource = new Mock<CoapResource>("/test") { CallBase = true };
            mockResource
                .Setup(r => r.Put(It.IsAny<CoapMessage>()))
                .Returns(new CoapMessage())
                .Verifiable();

            var request = new CoapMessage { Code = CoapMessageCode.Put };
            request.SetUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();

            service.Resources.Add(mockResource.Object);
            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.ToBytes()).Wait();

            // Assert
            Mock.Verify(_endpoint, mockResource);
        }

        [Test]
        public void TestDeleteRequest()
        {
            // Arrange
            var mockResource = new Mock<CoapResource>("/test") { CallBase = true };
            mockResource
                .Setup(r => r.Delete(It.IsAny<CoapMessage>()))
                .Returns(new CoapMessage())
                .Verifiable();

            var request = new CoapMessage { Code = CoapMessageCode.Delete };
            request.SetUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();

            service.Resources.Add(mockResource.Object);
            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.ToBytes()).Wait();

            // Assert
            Mock.Verify(_endpoint, mockResource);
        }

        [Test]
        public void TestResourceMethodNotImplemented()
        {
            // Arrange
            var expectedMessage = CoapMessage.FromException(new NotImplementedException()).ToBytes();

            _endpoint
                .Setup(c => c.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expectedMessage)), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(0))
                .Verifiable();

            var mockResource = new Mock<CoapResource>("/test") { CallBase = true };
            mockResource
                .Setup(r => r.Get(It.IsAny<CoapMessage>()))
                .Throws(new NotImplementedException());

            var request = new CoapMessage { Code = CoapMessageCode.Get };
            request.SetUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();

            service.Resources.Add(mockResource.Object);
            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.ToBytes()).Wait();

            // Assert
            Mock.Verify(_endpoint);
        }

        [Test]
        public void TestResourceMethodBadOption()
        {
            // Arrange
            var expectedMessage = CoapMessage.FromException(new CoapOptionException("Unsupported critical option (45575)")).ToBytes();

            _endpoint
                .Setup(c => c.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expectedMessage)), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(0))
                .Verifiable();

            var mockResource = new Mock<CoapResource>("/test") { CallBase = true };
            mockResource
                .Setup(r => r.Get(It.IsAny<CoapMessage>()))
                .Throws(new NotImplementedException());

            var request = new CoapMessage {Code = CoapMessageCode.Get, Options = {new CoapOption(45575) }};
            request.SetUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();

            service.Resources.Add(mockResource.Object);
            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.ToBytes()).Wait();

            // Assert
            Mock.Verify(_endpoint);
        }

        [Test]
        public void TestResourceNotFound()
        {
            // Arrange
            var expectedMessage = CoapMessage.Create(CoapMessageCode.NotFound, $"Resouce {new Uri(_baseUri, "/test")} was not found", CoapMessageType.Acknowledgement).ToBytes();

            _endpoint
                .Setup(e => e.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expectedMessage)), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(0))
                .Verifiable();

            var request = new CoapMessage { Code = CoapMessageCode.Get };
            request.SetUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();

            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.ToBytes()).Wait();

            // Assert
            Mock.Verify(_endpoint);
        }

        [Test]
        public void TestResourceInternalError()
        {
            // Arrange
            var expectedMessage = CoapMessage.Create(CoapMessageCode.InternalServerError, "An unexpected error occured", CoapMessageType.Reset).ToBytes();

            _endpoint
                .Setup(e => e.SendAsync(It.Is<CoapPacket>(p => p.Payload.SequenceEqual(expectedMessage)), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(0))
                .Verifiable();

            var mockResource = new Mock<CoapResource>("/test") { CallBase = true };
            mockResource
                .Setup(r => r.Get(It.IsAny<CoapMessage>()))
                .Throws(new Exception("Somne generic exception"));

            var request = new CoapMessage { Code = CoapMessageCode.Get };
            request.SetUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapResourceHandler();
            service.Resources.Add(mockResource.Object);

            Assert.Throws<Exception>(()=> service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.ToBytes()).GetAwaiter().GetResult());

            // Assert
            Mock.Verify(_endpoint);
        }
    }
}