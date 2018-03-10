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
using System.Net;
using NUnit.Framework;
using System.Collections;

namespace CoAPNet.Tests
{
    [TestFixture]
    public class CoapMiscellaneous
    {
        [TestCase(1, ExpectedResult = "FF01::FD")]
        [TestCase(2, ExpectedResult = "FF02::FD")]
        [TestCase(3, ExpectedResult = "FF03::FD")]
        [TestCase(4, ExpectedResult = "FF04::FD")]
        [TestCase(5, ExpectedResult = "FF05::FD")]
        [TestCase(6, ExpectedResult = "FF06::FD")]
        [TestCase(7, ExpectedResult = "FF07::FD")]
        [TestCase(8, ExpectedResult = "FF08::FD")]
        [TestCase(9, ExpectedResult = "FF09::FD")]
        [TestCase(10, ExpectedResult = "FF0A::FD")]
        [TestCase(11, ExpectedResult = "FF0B::FD")]
        [TestCase(12, ExpectedResult = "FF0C::FD")]
        [TestCase(13, ExpectedResult = "FF0D::FD")]
        [TestCase(14, ExpectedResult = "FF0E::FD")]
        public string TestMulticastIPv6Scopes(int scope)
        {
            return Coap.GetMulticastIPv6ForScope(scope);
        }

        [TestCase(0)]
        [TestCase(15)]
        public void TestMulticastIPv6ScopesThrowsException(int scope)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Coap.GetMulticastIPv6ForScope(scope));
        }

        public static IEnumerable IsRequestTestCases
        {
            get
            {
                yield return new TestCaseData(CoapMessageCode.None).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Get).Returns(true);
                yield return new TestCaseData(CoapMessageCode.Post).Returns(true);
                yield return new TestCaseData(CoapMessageCode.Put).Returns(true);
                yield return new TestCaseData(CoapMessageCode.Delete).Returns(true);
                yield return new TestCaseData(CoapMessageCode.Created).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Deleted).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Valid).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Changed).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Content).Returns(false);
                yield return new TestCaseData(CoapMessageCode.BadRequest).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Unauthorized).Returns(false);
                yield return new TestCaseData(CoapMessageCode.BadOption).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Forbidden).Returns(false);
                yield return new TestCaseData(CoapMessageCode.NotFound).Returns(false);
                yield return new TestCaseData(CoapMessageCode.MethodNotAllowed).Returns(false);
                yield return new TestCaseData(CoapMessageCode.NotAcceptable).Returns(false);
                yield return new TestCaseData(CoapMessageCode.PreconditionFailed).Returns(false);
                yield return new TestCaseData(CoapMessageCode.RequestEntityTooLarge).Returns(false);
                yield return new TestCaseData(CoapMessageCode.UnsupportedContentFormat).Returns(false);
                yield return new TestCaseData(CoapMessageCode.InternalServerError).Returns(false);
                yield return new TestCaseData(CoapMessageCode.NotImplemented).Returns(false);
                yield return new TestCaseData(CoapMessageCode.BadGateway).Returns(false);
                yield return new TestCaseData(CoapMessageCode.ServiceUnavailable).Returns(false);
                yield return new TestCaseData(CoapMessageCode.GatewayTimeout).Returns(false);
                yield return new TestCaseData(CoapMessageCode.ProxyingNotSupported).Returns(false);
            }
        }

        [TestCaseSource(nameof(IsRequestTestCases))]
        public bool TestCoapMessageCodeIsRequest(CoapMessageCode code)
        {
            return code.IsRequest();
        }

        public static IEnumerable IsSuccessTestCases
        {
            get
            {
                yield return new TestCaseData(CoapMessageCode.None).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Get).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Post).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Put).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Delete).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Created).Returns(true);
                yield return new TestCaseData(CoapMessageCode.Deleted).Returns(true);
                yield return new TestCaseData(CoapMessageCode.Valid).Returns(true);
                yield return new TestCaseData(CoapMessageCode.Changed).Returns(true);
                yield return new TestCaseData(CoapMessageCode.Content).Returns(true);
                yield return new TestCaseData(CoapMessageCode.BadRequest).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Unauthorized).Returns(false);
                yield return new TestCaseData(CoapMessageCode.BadOption).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Forbidden).Returns(false);
                yield return new TestCaseData(CoapMessageCode.NotFound).Returns(false);
                yield return new TestCaseData(CoapMessageCode.MethodNotAllowed).Returns(false);
                yield return new TestCaseData(CoapMessageCode.NotAcceptable).Returns(false);
                yield return new TestCaseData(CoapMessageCode.PreconditionFailed).Returns(false);
                yield return new TestCaseData(CoapMessageCode.RequestEntityTooLarge).Returns(false);
                yield return new TestCaseData(CoapMessageCode.UnsupportedContentFormat).Returns(false);
                yield return new TestCaseData(CoapMessageCode.InternalServerError).Returns(false);
                yield return new TestCaseData(CoapMessageCode.NotImplemented).Returns(false);
                yield return new TestCaseData(CoapMessageCode.BadGateway).Returns(false);
                yield return new TestCaseData(CoapMessageCode.ServiceUnavailable).Returns(false);
                yield return new TestCaseData(CoapMessageCode.GatewayTimeout).Returns(false);
                yield return new TestCaseData(CoapMessageCode.ProxyingNotSupported).Returns(false);
            }
        }

        [TestCaseSource(nameof(IsSuccessTestCases))]
        public bool TestCoapMessageCodeIsSuccess(CoapMessageCode code)
        {
            return code.IsSuccess();
        }

        public static IEnumerable IsClientErrorTestCases
        {
            get
            {
                yield return new TestCaseData(CoapMessageCode.None).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Get).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Post).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Put).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Delete).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Created).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Deleted).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Valid).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Changed).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Content).Returns(false);
                yield return new TestCaseData(CoapMessageCode.BadRequest).Returns(true);
                yield return new TestCaseData(CoapMessageCode.Unauthorized).Returns(true);
                yield return new TestCaseData(CoapMessageCode.BadOption).Returns(true);
                yield return new TestCaseData(CoapMessageCode.Forbidden).Returns(true);
                yield return new TestCaseData(CoapMessageCode.NotFound).Returns(true);
                yield return new TestCaseData(CoapMessageCode.MethodNotAllowed).Returns(true);
                yield return new TestCaseData(CoapMessageCode.NotAcceptable).Returns(true);
                yield return new TestCaseData(CoapMessageCode.PreconditionFailed).Returns(true);
                yield return new TestCaseData(CoapMessageCode.RequestEntityTooLarge).Returns(true);
                yield return new TestCaseData(CoapMessageCode.UnsupportedContentFormat).Returns(true);
                yield return new TestCaseData(CoapMessageCode.InternalServerError).Returns(false);
                yield return new TestCaseData(CoapMessageCode.NotImplemented).Returns(false);
                yield return new TestCaseData(CoapMessageCode.BadGateway).Returns(false);
                yield return new TestCaseData(CoapMessageCode.ServiceUnavailable).Returns(false);
                yield return new TestCaseData(CoapMessageCode.GatewayTimeout).Returns(false);
                yield return new TestCaseData(CoapMessageCode.ProxyingNotSupported).Returns(false);
            }
        }

        [TestCaseSource(nameof(IsClientErrorTestCases))]
        public bool TestCoapMessageCodeIsClientError(CoapMessageCode code)
        {
            return code.IsClientError();
        }

        public static IEnumerable IsServerErrorTestCases
        {
            get
            {
                yield return new TestCaseData(CoapMessageCode.None).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Get).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Post).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Put).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Delete).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Created).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Deleted).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Valid).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Changed).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Content).Returns(false);
                yield return new TestCaseData(CoapMessageCode.BadRequest).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Unauthorized).Returns(false);
                yield return new TestCaseData(CoapMessageCode.BadOption).Returns(false);
                yield return new TestCaseData(CoapMessageCode.Forbidden).Returns(false);
                yield return new TestCaseData(CoapMessageCode.NotFound).Returns(false);
                yield return new TestCaseData(CoapMessageCode.MethodNotAllowed).Returns(false);
                yield return new TestCaseData(CoapMessageCode.NotAcceptable).Returns(false);
                yield return new TestCaseData(CoapMessageCode.PreconditionFailed).Returns(false);
                yield return new TestCaseData(CoapMessageCode.RequestEntityTooLarge).Returns(false);
                yield return new TestCaseData(CoapMessageCode.UnsupportedContentFormat).Returns(false);
                yield return new TestCaseData(CoapMessageCode.InternalServerError).Returns(true);
                yield return new TestCaseData(CoapMessageCode.NotImplemented).Returns(true);
                yield return new TestCaseData(CoapMessageCode.BadGateway).Returns(true);
                yield return new TestCaseData(CoapMessageCode.ServiceUnavailable).Returns(true);
                yield return new TestCaseData(CoapMessageCode.GatewayTimeout).Returns(true);
                yield return new TestCaseData(CoapMessageCode.ProxyingNotSupported).Returns(true);
            }
        }

        [TestCaseSource(nameof(IsServerErrorTestCases))]
        public bool TestCoapMessageCodeIsServerError(CoapMessageCode code)
        {
            return code.IsServerError();
        }

        public static IEnumerable CompareMessageIdTestCases
        {
            get
            {
                yield return new TestCaseData(
                        new CoapMessageIdentifier(1234, CoapMessageType.Confirmable, new byte[] { 1, 2, 3, 4 }, new CoapEndpoint() { BaseUri = new Uri("coap://1.2.3.4:1234/") }, true),
                        new CoapMessageIdentifier(1234, CoapMessageType.Acknowledgement, new byte[] { 1, 2, 3, 4 }, new CoapEndpoint() { BaseUri = new Uri("coap://1.2.3.4:1234/") }, false))
                    .Returns(true);

                yield return new TestCaseData(
                        new CoapMessageIdentifier(1234, CoapMessageType.Confirmable, new byte[] { 1, 2, 3, 4 }, new CoapEndpoint() { BaseUri = new Uri("coap://1.2.3.4:1234/") }, true),
                        new CoapMessageIdentifier(5678, CoapMessageType.Confirmable, new byte[] { 1, 2, 3, 4 }, new CoapEndpoint() { BaseUri = new Uri("coap://1.2.3.4:1234/") }, false))
                    .Returns(true);
            }
        }

        [TestCaseSource(nameof(CompareMessageIdTestCases))]
        public bool CompareMessageId(CoapMessageIdentifier messageIdA, CoapMessageIdentifier messageIdB)
        {
            return messageIdA.Equals(messageIdB);
        }
    }
}