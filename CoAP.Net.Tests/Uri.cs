using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoAP.Net.Tests
{
    [TestClass]
    public class UriTest
    {
        [TestMethod]
        public void TestMessageFromUri()
        {
            var message = new Message();
            message.FromUri("coap://example.net/.well-known/core");

            var expectedOptions = new List<Option>
            {
                new Options.UriHost{ValueString="example.net"},
                new Options.UriPath{ValueString=".well-known"},
                new Options.UriPath{ValueString="core"},
            };

            Assert.IsTrue(expectedOptions.SequenceEqual(message.Options));
        }

        [TestMethod]
        public void TestMessageFromUriIPv4()
        {
            var message = new Message();
            message.FromUri("coap://198.51.100.1:61616//%2F//?%2F%2F&?%26");

            var expectedOptions = new List<Option> {
                new Options.UriPort{ValueUInt=61616},
                new Options.UriPath{ValueString=""},
                new Options.UriPath{ValueString="/"},
                new Options.UriPath{ValueString=""},
                new Options.UriPath{ValueString=""},
                new Options.UriQuery{ValueString="//"},
                new Options.UriQuery{ValueString="?&"},
            };

            Assert.IsTrue(expectedOptions.SequenceEqual(message.Options));
        }

        [TestMethod]
        public void TestMessageFromUriSpecialChars()
        {
            var message = new Message();
            message.FromUri("coap://ほげ.example/%E3%81%93%E3%82%93%E3%81%AB%E3%81%A1%E3%81%AF");

            var expectedOptions = new List<Option>
            {
                new Options.UriHost{ValueString="xn--18j4d.example"},
                new Options.UriPath{ValueString="\u3053\u3093\u306b\u3061\u306f"},
            };

            Assert.IsTrue(expectedOptions.SequenceEqual(message.Options));
        }
    }
}
