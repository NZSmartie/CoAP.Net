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
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

using CoAPNet.Server;
using CoAPNet.Tests.Mocks;

namespace CoAPNet.Tests
{
    [TestFixture]
    public class CoapHandlerTests
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

            var request = new CoapMessage { Code = CoapMessageCode.Get };
            request.SetUri(new Uri(_baseUri, "/test"));

            // Act
            var service = new CoapHandler();

            service.ProcessRequestAsync(new MockConnectionInformation(_endpoint.Object), request.ToBytes()).Wait();

            // Assert
            Mock.Verify(_endpoint);
        }
    }
}