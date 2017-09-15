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
using NUnit.Framework;

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

        [TestCase(CoapMessageCode.None, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Get, ExpectedResult = true)]
        [TestCase(CoapMessageCode.Post, ExpectedResult = true)]
        [TestCase(CoapMessageCode.Put, ExpectedResult = true)]
        [TestCase(CoapMessageCode.Delete, ExpectedResult = true)]
        [TestCase(CoapMessageCode.Created, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Deleted, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Valid, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Changed, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Content, ExpectedResult = false)]
        [TestCase(CoapMessageCode.BadRequest, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Unauthorized, ExpectedResult = false)]
        [TestCase(CoapMessageCode.BadOption, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Forbidden, ExpectedResult = false)]
        [TestCase(CoapMessageCode.NotFound, ExpectedResult = false)]
        [TestCase(CoapMessageCode.MethodNotAllowed, ExpectedResult = false)]
        [TestCase(CoapMessageCode.NotAcceptable, ExpectedResult = false)]
        [TestCase(CoapMessageCode.PreconditionFailed, ExpectedResult = false)]
        [TestCase(CoapMessageCode.RequestEntityTooLarge, ExpectedResult = false)]
        [TestCase(CoapMessageCode.UnsupportedContentFormat, ExpectedResult = false)]
        [TestCase(CoapMessageCode.InternalServerError, ExpectedResult = false)]
        [TestCase(CoapMessageCode.NotImplemented, ExpectedResult = false)]
        [TestCase(CoapMessageCode.BadGateway, ExpectedResult = false)]
        [TestCase(CoapMessageCode.ServiceUnavailable, ExpectedResult = false)]
        [TestCase(CoapMessageCode.GatewayTimeout, ExpectedResult = false)]
        [TestCase(CoapMessageCode.ProxyingNotSupported, ExpectedResult = false)]
        public bool TestCoapMessageCodeIsRequest(CoapMessageCode code)
        {
            return code.IsRequest();
        }

        [TestCase(CoapMessageCode.None, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Get, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Post, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Put, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Delete, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Created, ExpectedResult = true)]
        [TestCase(CoapMessageCode.Deleted, ExpectedResult = true)]
        [TestCase(CoapMessageCode.Valid, ExpectedResult = true)]
        [TestCase(CoapMessageCode.Changed, ExpectedResult = true)]
        [TestCase(CoapMessageCode.Content, ExpectedResult = true)]
        [TestCase(CoapMessageCode.BadRequest, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Unauthorized, ExpectedResult = false)]
        [TestCase(CoapMessageCode.BadOption, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Forbidden, ExpectedResult = false)]
        [TestCase(CoapMessageCode.NotFound, ExpectedResult = false)]
        [TestCase(CoapMessageCode.MethodNotAllowed, ExpectedResult = false)]
        [TestCase(CoapMessageCode.NotAcceptable, ExpectedResult = false)]
        [TestCase(CoapMessageCode.PreconditionFailed, ExpectedResult = false)]
        [TestCase(CoapMessageCode.RequestEntityTooLarge, ExpectedResult = false)]
        [TestCase(CoapMessageCode.UnsupportedContentFormat, ExpectedResult = false)]
        [TestCase(CoapMessageCode.InternalServerError, ExpectedResult = false)]
        [TestCase(CoapMessageCode.NotImplemented, ExpectedResult = false)]
        [TestCase(CoapMessageCode.BadGateway, ExpectedResult = false)]
        [TestCase(CoapMessageCode.ServiceUnavailable, ExpectedResult = false)]
        [TestCase(CoapMessageCode.GatewayTimeout, ExpectedResult = false)]
        [TestCase(CoapMessageCode.ProxyingNotSupported, ExpectedResult = false)]
        public bool TestCoapMessageCodeIsSuccess(CoapMessageCode code)
        {
            return code.IsSuccess();
        }

        [TestCase(CoapMessageCode.None, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Get, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Post, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Put, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Delete, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Created, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Deleted, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Valid, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Changed, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Content, ExpectedResult = false)]
        [TestCase(CoapMessageCode.BadRequest, ExpectedResult = true)]
        [TestCase(CoapMessageCode.Unauthorized, ExpectedResult = true)]
        [TestCase(CoapMessageCode.BadOption, ExpectedResult = true)]
        [TestCase(CoapMessageCode.Forbidden, ExpectedResult = true)]
        [TestCase(CoapMessageCode.NotFound, ExpectedResult = true)]
        [TestCase(CoapMessageCode.MethodNotAllowed, ExpectedResult = true)]
        [TestCase(CoapMessageCode.NotAcceptable, ExpectedResult = true)]
        [TestCase(CoapMessageCode.PreconditionFailed, ExpectedResult = true)]
        [TestCase(CoapMessageCode.RequestEntityTooLarge, ExpectedResult = true)]
        [TestCase(CoapMessageCode.UnsupportedContentFormat, ExpectedResult = true)]
        [TestCase(CoapMessageCode.InternalServerError, ExpectedResult = false)]
        [TestCase(CoapMessageCode.NotImplemented, ExpectedResult = false)]
        [TestCase(CoapMessageCode.BadGateway, ExpectedResult = false)]
        [TestCase(CoapMessageCode.ServiceUnavailable, ExpectedResult = false)]
        [TestCase(CoapMessageCode.GatewayTimeout, ExpectedResult = false)]
        [TestCase(CoapMessageCode.ProxyingNotSupported, ExpectedResult = false)]
        public bool TestCoapMessageCodeIsClientError(CoapMessageCode code)
        {
            return code.IsClientError();
        }

        [TestCase(CoapMessageCode.None, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Get, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Post, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Put, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Delete, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Created, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Deleted, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Valid, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Changed, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Content, ExpectedResult = false)]
        [TestCase(CoapMessageCode.BadRequest, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Unauthorized, ExpectedResult = false)]
        [TestCase(CoapMessageCode.BadOption, ExpectedResult = false)]
        [TestCase(CoapMessageCode.Forbidden, ExpectedResult = false)]
        [TestCase(CoapMessageCode.NotFound, ExpectedResult = false)]
        [TestCase(CoapMessageCode.MethodNotAllowed, ExpectedResult = false)]
        [TestCase(CoapMessageCode.NotAcceptable, ExpectedResult = false)]
        [TestCase(CoapMessageCode.PreconditionFailed, ExpectedResult = false)]
        [TestCase(CoapMessageCode.RequestEntityTooLarge, ExpectedResult = false)]
        [TestCase(CoapMessageCode.UnsupportedContentFormat, ExpectedResult = false)]
        [TestCase(CoapMessageCode.InternalServerError, ExpectedResult = true)]
        [TestCase(CoapMessageCode.NotImplemented, ExpectedResult = true)]
        [TestCase(CoapMessageCode.BadGateway, ExpectedResult = true)]
        [TestCase(CoapMessageCode.ServiceUnavailable, ExpectedResult = true)]
        [TestCase(CoapMessageCode.GatewayTimeout, ExpectedResult = true)]
        [TestCase(CoapMessageCode.ProxyingNotSupported, ExpectedResult = true)]
        public bool TestCoapMessageCodeIsServerError(CoapMessageCode code)
        {
            return code.IsServerError();
        }
    }
}