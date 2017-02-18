using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoAP.Net.Tests
{
    [TestClass]
    public class MessageTest
    {
        private Message _message = null;

        [TestInitialize]
        public void InitTest()
        {
            _message = new Message();
        }

        [TestCleanup]
        public void CleanupTest()
        {
            _message = null;
        }

        [TestCategory("Messages"), TestCategory("Encoding")]
        [TestMethod]
        public void TestMessageEmpty()
        {
            _message.Type = MessageType.Confirmable;
            _message.Code = MessageCode.None;
            _message.Id = 1234;

            var expected = new byte[] { 0x40, 0x00, 0x04, 0xD2 };
            var actual = _message.Serialise();

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void TestMessageEmptyAckknowledgement()
        {
            _message = new Message();
            _message.Type = MessageType.Acknowledgement;
            _message.Code = MessageCode.Valid;
            _message.Id = 1235;

            var expected = new byte[] { 0x60, 0x43, 0x04, 0xD3 };
            var actual = _message.Serialise();

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestCategory("Messages"), TestCategory("Encoding")]
        [TestMethod]
        public void TestMessageEncodeRequest()
        {
            _message.Type = MessageType.Confirmable;
            _message.Code = MessageCode.Get;
            _message.Id = 16962;
            _message.Token = new byte[] { 0xde, 0xad, 0xbe, 0xef };

            _message.Options.Add(new Options.UriPath(".well-known"));
            _message.Options.Add(new Options.UriPath("core"));

            var expected = new byte[] {
                0x44, 0x01, 0x42, 0x42, 0xde, 0xad, 0xbe, 0xef, 0xBB, 0x2E, 0x77, 0x65, 0x6C, 0x6C, 0x2D,
                0x6B, 0x6E, 0x6F, 0x77, 0x6E, 0x04, 0x63, 0x6F, 0x72, 0x65
            };
            var actual = _message.Serialise();

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestCategory("Messages"), TestCategory("Encoding")]
        [TestMethod]
        public void TestMessageEncodeResponse()
        {
            _message.Type = MessageType.Acknowledgement;
            _message.Code = MessageCode.Content;
            _message.Id = 16962;
            _message.Token = new byte[] { 0xde, 0xad, 0xbe, 0xef };
            _message.Options.Add(new Options.ContentFormat(Options.ContentFormatType.ApplicationLinkFormat));

            _message.Payload = Encoding.UTF8.GetBytes("<.well-known/core/>");

            var expected = new byte[] {
                0x64, 0x45, 0x42, 0x42, 0xde, 0xad, 0xbe, 0xef, 0xc1, 0x28, 0xff, 0x3c, 0x2e, 0x77, 0x65, 0x6c, 0x6c, 0x2d, 0x6b, 0x6e,
                0x6f, 0x77, 0x6e, 0x2f, 0x63, 0x6f, 0x72, 0x65, 0x2f, 0x3e
            };
            var actual = _message.Serialise();

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestCategory("Messages"), TestCategory("Options")]
        [TestMethod]
        public void TestMessageFromUri()
        {
            _message.FromUri("coap://example.net/.well-known/core");

            var expectedOptions = new List<Option>
            {
                new Options.UriHost("example.net"),
                new Options.UriPath(".well-known"),
                new Options.UriPath("core"),
            };

            Assert.IsTrue(expectedOptions.SequenceEqual(_message.Options));

            // Test again but using static CreateFromUri method
            var message = Message.CreateFromUri("coap://example.net/.well-known/core");
            Assert.IsTrue(expectedOptions.SequenceEqual(message.Options));
        }

        [TestCategory("Messages"), TestCategory("Options")]
        [TestMethod]
        public void TestMessageFromUriIPv4()
        {
            _message.FromUri("coap://198.51.100.1:61616//%2F//?%2F%2F&?%26");

            var expectedOptions = new List<Option> {
                new Options.UriPort(61616),
                new Options.UriPath(""),
                new Options.UriPath("/"),
                new Options.UriPath(""),
                new Options.UriPath(""),
                new Options.UriQuery("//"),
                new Options.UriQuery("?&"),
            };

            Assert.IsTrue(expectedOptions.SequenceEqual(_message.Options));

            // Test again but using static CreateFromUri method
            var message = Message.CreateFromUri("coap://198.51.100.1:61616//%2F//?%2F%2F&?%26");
            Assert.IsTrue(expectedOptions.SequenceEqual(message.Options));
        }

        [TestCategory("Messages"), TestCategory("Options")]
        [TestMethod]
        public void TestMessageFromUriSpecialChars()
        {
            _message.FromUri("coap://\u307B\u3052.example/%E3%81%93%E3%82%93%E3%81%AB%E3%81%A1%E3%81%AF");

            var expectedOptions = new List<Option>
            {
                new Options.UriHost("xn--18j4d.example"),
                new Options.UriPath("\u3053\u3093\u306b\u3061\u306f"),
            };

            Assert.IsTrue(expectedOptions.SequenceEqual(_message.Options));

            // Test again but using static CreateFromUri method
            var message = Message.CreateFromUri("coap://\u307B\u3052.example/%E3%81%93%E3%82%93%E3%81%AB%E3%81%A1%E3%81%AF");
            Assert.IsTrue(expectedOptions.SequenceEqual(message.Options));
        }
    }
}
