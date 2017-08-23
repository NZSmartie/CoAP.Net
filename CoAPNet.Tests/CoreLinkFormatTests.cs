using System;
using System.Collections.Generic;
using System.Linq;
using NUnit;
using NUnit.Framework;

using CoAPNet;

namespace CoAPNet.Tests
{
    [TestFixture]
    public class CoreLinkFormatTests
    {
        [Test]
        [Category("[RFC6690] Section 2")]
        public void ParseSimpleLinkFormat()
        {
            // Arrange
            var expected = new List<CoapResourceMetadata>
            {
                new CoapResourceMetadata("/sensor/temp")
                {
                    InterfaceDescription = { "sensor" }
                },
                new CoapResourceMetadata("/sensor/light")
                {
                    InterfaceDescription = { "sensor" }
                }
            };

            var message = "</sensor/temp>;if=\"sensor\",</sensor/light>;if=\"sensor\"";

            // Act
            var actual = CoreLinkFormat.Parse(message);

            // Assert
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [Test]
        [Category("[RFC6690] Section 2")]
        public void ParseExtensiveLinkFormat()
        {
            // Arrange
            var expected = new List<CoapResourceMetadata>
            {
                new CoapResourceMetadata("/sensor/temp")
                {
                    InterfaceDescription = {"sensor", "read"},
                    ResourceTypes = { "temperature-c", "temperature-f" },
                    Rev = {"one", "two" },
                    Rel = {"one", "two" },
                    HrefLang = "en-nz",
                    Media = "none",
                    Title = "Outside Temperature",
                    TitleExt = "Primo Sensor",
                    SuggestedContentTypes = 
                    {
                        Options.ContentFormatType.TextPlain,
                        Options.ContentFormatType.ApplicationJson
                    }
                },
                new CoapResourceMetadata("http://stupid.schema.io/temperature.json")
                {
                    Anchor = "/sensor/temp"
                },
                new CoapResourceMetadata("/firmware/v2.1")
                {
                    ResourceTypes = { "firmware" },
                    SuggestedContentTypes = { Options.ContentFormatType.ApplicationOctetStream },
                    MaxSize = 262144
                }
            };

            var message = "</sensor/temp>;if=\"sensor read\";ct=\"0 50\";rt=\"temperature-c temperature-f\";rev=\"one two\";rel=\"one two\";hreflang=en-nz;media=none;title=\"Outside Temperature\";title*=utf-8'en-nz'\"Primo Sensor\""
                + ",<http://stupid.schema.io/temperature.json>;anchor=\"/sensor/temp\""
                + ",</firmware/v2.1>;rt=\"firmware\";ct=42;sz=262144";

            // Act
            var actual = CoreLinkFormat.Parse(message);

            // Assert
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [Test]
        [Category("[RFC6690] Section 2")]
        public void SimpleLinkFormat()
        {
            // Arrange
            var expected = "</sensor/temp>;if=\"sensor\",</sensor/light>;if=\"sensor\"";

            var resources = new List<CoapResourceMetadata>
            {
                new CoapResourceMetadata("/sensor/temp")
                {
                    InterfaceDescription = { "sensor" }
                },
                new CoapResourceMetadata("/sensor/light")
                {
                    InterfaceDescription = { "sensor" }
                }
            };

            // Act
            var actual = CoreLinkFormat.ToCoreLinkFormat(resources);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        [Category("[RFC6690] Section 2")]
        public void ExtensiveLinkFormat()
        {
            // Arrange
            var expected = "</sensor/temp>;if=\"sensor read\";ct=\"0 50\";rt=\"temperature-c temperature-f\";rev=\"one two\";rel=\"one two\";hreflang=en-nz;media=none;title=\"Outside Temperature\""
                           + ",</firmware/v2.1>;ct=42;rt=\"firmware\";sz=262144";

            var resources = new List<CoapResourceMetadata>
            {
                new CoapResourceMetadata("/sensor/temp")
                {
                    InterfaceDescription = {"sensor", "read"},
                    ResourceTypes = { "temperature-c", "temperature-f" },
                    Rev = {"one", "two" },
                    Rel = {"one", "two" },
                    HrefLang = "en-nz",
                    Media = "none",
                    Title = "Outside Temperature",
                    TitleExt = "Primo Sensor",
                    SuggestedContentTypes =
                    {
                        Options.ContentFormatType.TextPlain,
                        Options.ContentFormatType.ApplicationJson
                    }
                },
                new CoapResourceMetadata("/firmware/v2.1")
                {
                    ResourceTypes = { "firmware" },
                    SuggestedContentTypes = { Options.ContentFormatType.ApplicationOctetStream },
                    MaxSize = 262144
                }
            };

            // Act
            var actual = CoreLinkFormat.ToCoreLinkFormat(resources);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        [Category("[RFC6690] Section 2")]
        public void AncoredLinkFormat()
        {
            // Arrange
            var expected = "</sensor/temp>;if=\"sensor\";rt=\"temperature-c\""
                           + ",<http://stupid.schema.io/temperature.json>;anchor=\"/sensor/temp\"";

            var resources = new List<CoapResourceMetadata>
            {
                new CoapResourceMetadata("/sensor/temp")
                {
                    InterfaceDescription = {"sensor"},
                    ResourceTypes = { "temperature-c"}
                },
                new CoapResourceMetadata("http://stupid.schema.io/temperature.json")
                {
                    Anchor = "/sensor/temp"
                },
            };

            // Act
            var actual = CoreLinkFormat.ToCoreLinkFormat(resources);

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
