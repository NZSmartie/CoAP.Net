using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CoAP.Net;

namespace CoAP.Net.Tests
{
    [TestClass]
    public class TestCoreLinkFormat
    {
        [TestMethod]
        [TestCategory("[RFC6690] Section 2")]
        public void ParseSimpleLinkFormat()
        {
            // Arrange
            var expected = new List<CoapResource>
            {
                new CoapResource("/sensor/temp")
                {
                    InterfaceDescription = new List<string>{ "sensor" }
                },
                new CoapResource("/sensor/light")
                {
                    InterfaceDescription = new List<string>{ "sensor" }
                }
            };

            var message = "</sensor/temp>;if=\"sensor\",</sensor/light>;if=\"sensor\"";

            // Act
            var actual = CoreLinkFormat.Parse(message);

            // Assert
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        [TestCategory("[RFC6690] Section 2")]
        public void ParseExtensiveLinkFormat()
        {
            // Arrange
            var expected = new List<CoapResource>
            {
                new CoapResource("/sensor/temp")
                {
                    InterfaceDescription = new List<string>{"sensor", "read"},
                    ResourceTypes = new List<string> { "temperature-c", "temperature-f" },
                    Rev = new List<string>{"one", "two" },
                    Rel = new List<string>{"one", "two" },
                    HrefLang = "en-nz",
                    Media = "none",
                    Title = "Outside Temperature",
                    TitleExt = "Primo Sensor"
                },
                new CoapResource("http://stupid.schema.io/temperature.json")
                {
                    Anchor = "/sensor/temp"
                },
                new CoapResource("/firmware/v2.1")
                {
                    ResourceTypes = new List<string>{ "firmware" },
                    MaxSize = 262144
                }
            };

            var message = "</sensor/temp>;if=\"sensor read\";rt=\"temperature-c temperature-f\";rev=\"one two\";rel=\"one two\";hreflang=en-nz;media=none;title=\"Outside Temperature\";title*=utf-8'en-nz'\"Primo Sensor\""
                + ",<http://stupid.schema.io/temperature.json>;anchor=\"/sensor/temp\""
                + ",</firmware/v2.1>;rt=\"firmware\";sz=262144";

            // Act
            var actual = CoreLinkFormat.Parse(message);

            // Assert
            Assert.IsTrue(expected.SequenceEqual(actual));
        }
    }
}
