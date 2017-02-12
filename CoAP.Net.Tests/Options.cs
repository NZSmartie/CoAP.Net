using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoAP.Net.Tests
{
    [TestClass]
    public class OptionsTest
    {
        [TestMethod]
        public void TestOptionIfMatch()
        {
            Option option = new Options.IfMatch();

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
            Assert.AreEqual(null, option.GetDefaultOpaque(), "Default value is incorrect");
        }

        [TestMethod]
        public void TestOptionUriHost()
        {
            Option option = new Options.UriHost();

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
            Assert.AreEqual(null, option.GetDefaultString(), "Default value is incorrect");
        }

        [TestMethod]
        public void TestOptionETag()
        {
            Option option = new Options.ETag();

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
            Assert.AreEqual(null, option.GetDefaultOpaque(), "Default value is incorrect");
        }

        [TestMethod]
        public void TestOptionIfNoneMatch()
        {
            Option option = new Options.IfNoneMatch();

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

        [TestMethod]
        public void TestOptionUriPort()
        {
            Option option = new Options.UriPort();

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
            Assert.AreEqual(0u, option.GetDefaultUInt(), "Default value is incorrect");
        }

        [TestMethod]
        public void TestOptionLocationPath()
        {
            Option option = new Options.LocationPath();

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
            Assert.AreEqual(null, option.GetDefaultString(), "Default value is incorrect");
        }

        [TestMethod]
        public void TestOptionUriPath()
        {
            Option option = new Options.UriPath();

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
            Assert.AreEqual(null, option.GetDefaultString(), "Default value is incorrect");
        }
        
        [TestMethod]
        public void TestOptionContentFormat()
        {
            Option option = new Options.ContentFormat();

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
            Assert.AreEqual(0u, option.GetDefaultUInt(), "Default value is incorrect");
        }

        [TestMethod]
        public void TestOptionMaxAge()
        {
            Option option = new Options.MaxAge();

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
            Assert.AreEqual(60u, option.GetDefaultUInt(), "Default value is incorrect");
        }

        [TestMethod]
        public void TestOptionUriQuery()
        {
            Option option = new Options.UriQuery();

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
            Assert.AreEqual(null, option.GetDefaultString(), "Default value is incorrect");
        }

        [TestMethod]
        public void TestOptionAccept()
        {
            Option option = new Options.Accept();

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
            Assert.AreEqual(0u, option.GetDefaultUInt(), "Default value is incorrect");
        }

        [TestMethod]
        public void TestOptionLocationQuery()
        {
            Option option = new Options.LocationQuery();

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
            Assert.AreEqual(null, option.GetDefaultString(), "Default value is incorrect");
        }

        [TestMethod]
        public void TestOptionProxyUri()
        {
            Option option = new Options.ProxyUri();

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
            Assert.AreEqual(null, option.GetDefaultString(), "Default value is incorrect");
        }

        [TestMethod]
        public void TestOptionProxyScheme()
        {
            Option option = new Options.ProxyScheme();

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
            Assert.AreEqual(null, option.GetDefaultString(), "Default value is incorrect");
        }

        [TestMethod]
        public void TestOptionSize1()
        {
            Option option = new Options.Size1();

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
            Assert.AreEqual(0u, option.GetDefaultUInt(), "Default value is incorrect");
        }
    }
}
