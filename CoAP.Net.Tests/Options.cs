using System;
using NUnit;
using NUnit.Framework;

namespace CoAPNet.Tests
{
    [TestFixture]
    public class OptionsTest
    {
        [Test]
        [Category("Options")]
        public void TestOptionHashCodesAndEquality()
        {
            // Should result in four items
            var setFourUriPath = new System.Collections.Generic.HashSet<CoapOption>
            {
                new Options.UriPath("one"),
                new Options.UriPath("two"),
                new Options.UriPath("three"),
                new Options.UriPath("four"),
            };

            // Should result in one item as duplicates are discarded
            var setOneUriPath = new System.Collections.Generic.HashSet<CoapOption>
            {
                new Options.UriPath("Test"),
                new Options.UriPath("Test"),
                new Options.UriPath("Test"),
                new Options.UriPath("Test")
            };

            // Should result in three items are hashcodes are different
            var setThreeDifferentOptions = new System.Collections.Generic.HashSet<CoapOption>
            {
                new Options.UriHost("Test"),
                new Options.UriPath("Test"),
                new Options.UriQuery("Test"),
            };

            Assert.AreEqual(4, setFourUriPath.Count);
            Assert.AreEqual(1, setOneUriPath.Count);
            Assert.AreEqual(3, setThreeDifferentOptions.Count);
        }

        [Test]
        [Category("[RFC7252] Section 5.10.8"), Category("Options")]
        public void TestOptionIfMatch()
        {
            CoapOption option = new Options.IfMatch();

            Assert.AreEqual(1, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(true, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(false, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(false, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(true, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(0, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(8, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.Opaque, option.Type, "Type is incorrect");
            Assert.AreEqual(null, option.DefaultOpaque, "Default value is incorrect");
        }

        [Test]
        [Category("[RFC7252] Section 5.10.1"), Category("Options")]
        public void TestOptionUriHost()
        {
            CoapOption option = new Options.UriHost();

            Assert.AreEqual(3, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(true, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(true, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(false, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(false, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(1, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(255, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.String, option.Type, "Type is incorrect");
            Assert.AreEqual(null, option.DefaultString, "Default value is incorrect");
        }

        [Test]
        [Category("[RFC7252] Section 5.10.6"), Category("Options")]
        public void TestOptionETag()
        {
            CoapOption option = new Options.ETag();

            Assert.AreEqual(4, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(false, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(false, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(false, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(true, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(1, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(8, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.Opaque, option.Type, "Type is incorrect");
            Assert.AreEqual(null, option.DefaultOpaque, "Default value is incorrect");
        }

        [Test]
        [Category("[RFC7252] Section 5.10.8"), Category("Options")]
        public void TestOptionIfNoneMatch()
        {
            CoapOption option = new Options.IfNoneMatch();

            Assert.AreEqual(5, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(true, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(false, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(false, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(false, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(0, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(0, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.Empty, option.Type, "Type is incorrect");
        }

        [Test]
        [Category("[RFC7252] Section 5.10.1"), Category("Options")]
        public void TestOptionUriPort()
        {
            CoapOption option = new Options.UriPort();

            Assert.AreEqual(7, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(true, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(true, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(false, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(false, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(0, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(2, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.UInt, option.Type, "Type is incorrect");
            Assert.AreEqual(0u, option.DefaultUInt, "Default value is incorrect");
        }

        [Test]
        [Category("[RFC7252] Section 5.10.7"), Category("Options")]
        public void TestOptionLocationPath()
        {
            CoapOption option = new Options.LocationPath();

            Assert.AreEqual(8, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(false, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(false, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(false, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(true, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(0, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(255, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.String, option.Type, "Type is incorrect");
            Assert.AreEqual(null, option.DefaultString, "Default value is incorrect");
        }

        [Test]
        [Category("[RFC7252] Section 5.10.1"), Category("Options")]
        public void TestOptionUriPath()
        {
            CoapOption option = new Options.UriPath();

            Assert.AreEqual(11, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(true, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(true, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(false, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(true, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(0, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(255, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.String, option.Type, "Type is incorrect");
            Assert.AreEqual(null, option.DefaultString, "Default value is incorrect");
        }
        
        [Test]
        [Category("[RFC7252] Section 5.10.3"), Category("Options")]
        public void TestOptionContentFormat()
        {
            CoapOption option = new Options.ContentFormat();

            Assert.AreEqual(12, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(false, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(false, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(false, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(false, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(0, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(2, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.UInt, option.Type, "Type is incorrect");
            Assert.AreEqual(0u, option.DefaultUInt, "Default value is incorrect");
        }

        [Test]
        [Category("[RFC7252] Section 5.10.5"), Category("Options")]
        public void TestOptionMaxAge()
        {
            CoapOption option = new Options.MaxAge();

            Assert.AreEqual(14, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(false, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(true, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(false, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(false, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(0, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(4, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.UInt, option.Type, "Type is incorrect");
            Assert.AreEqual(60u, option.DefaultUInt, "Default value is incorrect");
        }

        [Test]
        [Category("[RFC7252] Section 5.10.1"), Category("Options")]
        public void TestOptionUriQuery()
        {
            CoapOption option = new Options.UriQuery();

            Assert.AreEqual(15, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(true, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(true, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(false, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(true, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(0, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(255, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.String, option.Type, "Type is incorrect");
            Assert.AreEqual(null, option.DefaultString, "Default value is incorrect");
        }

        [Test]
        [Category("[RFC7252] Section 5.10.4"), Category("Options")]
        public void TestOptionAccept()
        {
            CoapOption option = new Options.Accept();

            Assert.AreEqual(17, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(true, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(false, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(false, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(false, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(0, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(2, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.UInt, option.Type, "Type is incorrect");
            Assert.AreEqual(0u, option.DefaultUInt, "Default value is incorrect");
        }

        [Test]
        [Category("[RFC7252] Section 5.10.7"), Category("Options")]
        public void TestOptionLocationQuery()
        {
            CoapOption option = new Options.LocationQuery();

            Assert.AreEqual(20, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(false, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(false, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(false, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(true, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(0, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(255, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.String, option.Type, "Type is incorrect");
            Assert.AreEqual(null, option.DefaultString, "Default value is incorrect");
        }

        [Test]
        [Category("[RFC7252] Section 5.10.2"), Category("Options")]
        public void TestOptionProxyUri()
        {
            CoapOption option = new Options.ProxyUri();

            Assert.AreEqual(35, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(true, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(true, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(false, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(false, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(1, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(1034, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.String, option.Type, "Type is incorrect");
            Assert.AreEqual(null, option.DefaultString, "Default value is incorrect");
        }

        [Test]
        [Category("[RFC7252] Section 5.10.2"), Category("Options")]
        public void TestOptionProxyScheme()
        {
            CoapOption option = new Options.ProxyScheme();

            Assert.AreEqual(39, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(true, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(true, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(false, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(false, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(1, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(255, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.String, option.Type, "Type is incorrect");
            Assert.AreEqual(null, option.DefaultString, "Default value is incorrect");
        }

        [Test]
        [Category("[RFC7252] Section 5.10.9"), Category("Options")]
        public void TestOptionSize1()
        {
            CoapOption option = new Options.Size1();

            Assert.AreEqual(60, option.OptionNumber, "OptionNumber is incorrect");

            // Check Option bahaviours
            Assert.AreEqual(false, option.IsCritical, "IsCritical is incorrect");
            Assert.AreEqual(false, option.IsUnsafe, "Isunsafe is incorrect");
            Assert.AreEqual(true, option.NoCacheKey, "NoCacheKey is incorrect");
            Assert.AreEqual(false, option.IsRepeatable, "IsRepeatable is incorrect");

            // Check Option Parameters
            Assert.AreEqual(0, option.MinLength, "MinLength is incorrect");
            Assert.AreEqual(4, option.MaxLength, "MaxLength is incorrect");

            Assert.AreEqual(OptionType.UInt, option.Type, "Type is incorrect");
            Assert.AreEqual(0u, option.DefaultUInt, "Default value is incorrect");
        }
    }
}
