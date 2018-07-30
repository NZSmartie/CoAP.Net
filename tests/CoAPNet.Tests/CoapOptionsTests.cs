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
using System.Collections;
using System.Text;
using NUnit;
using NUnit.Framework;

namespace CoAPNet.Tests
{
    [TestFixture]
    public class CoapOptionsTests
    {
        public static IEnumerable NullOptionComapreNullTestCases
        {
            get
            {
                yield return new TestCaseData(new CoapOption(1, type: OptionType.Empty));
                yield return new TestCaseData(new CoapOption(1, type: OptionType.Opaque));
                yield return new TestCaseData(new CoapOption(1, type: OptionType.String));
                yield return new TestCaseData(new CoapOption(1, type: OptionType.UInt));
            }
        }

        [TestCaseSource(nameof(NullOptionComapreNullTestCases))]
        public void NullOptionComapreNull(CoapOption option)
        {
            Assert.True(option.Equals(option));
        }

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

            Assert.AreEqual(new Options.ContentFormatType(50, "application/json"), Options.ContentFormatType.ApplicationJson, "Custom ContentFormatType doesn't match predefined Type");
            Assert.AreEqual(new Options.ContentFormatType(50, "Name shouldn't affect Equals"), Options.ContentFormatType.ApplicationJson, "Custom ContentFormatType with invalid name doesn't match predefined Type");
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

        [TestCase(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xF0 }, 8)]
        [TestCase(new byte[] { 0xAA, 0x55, 0x11 }, 3)]
        [TestCase(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c }, 13)]
        public void TestOpaqueOption(byte[] data, int length)
        {
            var option = new CoapOption(0, type: OptionType.Opaque, maxLength: 256);

            option.ValueOpaque = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xF0 };
            Assert.AreEqual(8, option.Length);
            Assert.AreEqual(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xF0 }, option.ValueOpaque);
            Assert.AreEqual(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xF0 }, option.GetBytes());
        }

        [TestCase(0u, 0, new byte[] { })]
        [TestCase(0x12u, 1, new byte[] {0x12})]
        [TestCase(0x1234u, 2, new byte[] {0x12, 0x34})]
        [TestCase(0x123456u, 3, new byte[] {0x12, 0x34, 0x56})]
        [TestCase(0x12345678u, 4, new byte[] {0x12, 0x34, 0x56, 0x78})]
        public void TestValueOption(uint value, int length, byte[] expected)
        {
            var optionToBytes = new CoapOption(0, type: OptionType.UInt, maxLength: 4);

            optionToBytes.ValueUInt = value;
            Assert.AreEqual(length, optionToBytes.Length);
            Assert.AreEqual(expected, optionToBytes.GetBytes());

            var optionFromBytes = new CoapOption(0, type: OptionType.UInt, maxLength: 4);

            optionFromBytes.FromBytes(expected);
            Assert.AreEqual(length, optionFromBytes.Length);
            Assert.AreEqual(value, optionFromBytes.ValueUInt);
        }

        [Test]
        public void TestOptionInvalidCastException()
        {
            var option = new CoapOption(0);

            Assert.Throws<InvalidCastException>(() => _ = option.ValueString);
            Assert.Throws<InvalidCastException>(() => _ = option.ValueString = "test");

            Assert.Throws<InvalidCastException>(() => _ = option.ValueOpaque);
            Assert.Throws<InvalidCastException>(() => _ = option.ValueOpaque = Encoding.UTF8.GetBytes("test"));

            Assert.Throws<InvalidCastException>(() => _ = option.ValueUInt);
            Assert.Throws<InvalidCastException>(() => _ = option.ValueUInt = 1234);

            Assert.Throws<InvalidCastException>(() => _ = option.DefaultString);
            Assert.Throws<InvalidCastException>(() => _ = option.DefaultOpaque);
            Assert.Throws<InvalidCastException>(() => _ = option.DefaultUInt);

            option.FromBytes(null); // No-op
            option.FromBytes(new byte[]{}); // No-op
            Assert.Throws<InvalidCastException>(() => option.FromBytes(new byte[]{0x12}));
        }

        [Category("[RFC7959] Section 2.2"), Category("Blocks")]
        [TestCase(0, 16, false, new byte[] { })]
        [TestCase(1, 16, false, new byte[] { 0x10 })]
        [TestCase(2, 32, false, new byte[] { 0x21 })]
        [TestCase(3, 64, true, new byte[] { 0x3A })]
        [TestCase(4095, 128, true, new byte[] { 0xFF, 0xFB })]
        [TestCase(1048575, 256, true, new byte[] { 0xFF, 0xFF, 0xFC})]
        [TestCase(15, 16, true, new byte[] { 0xF8 })]
        [TestCase(16, 16, true, new byte[] { 0x01, 0x08 })]
        [TestCase(39, 16, false, new byte[] { 0x02, 0x70 })]
        [TestCase(79, 16, false, new byte[] { 0x04, 0xF0 })]
        [TestCase(0, 128, true, new byte[] { 0x0b })]
        public void TestBlockOption(int blockNumber, int blockSize, bool more, byte[] expected)
        {
            var optionToBytes = new Options.Block1(blockNumber, blockSize, more);

            Assert.AreEqual(blockNumber, optionToBytes.BlockNumber, "Block Number");
            Assert.AreEqual(blockSize, optionToBytes.BlockSize, "Block Size");
            Assert.AreEqual(more, optionToBytes.IsMoreFollowing, "More Following");
            Assert.AreEqual(expected, optionToBytes.GetBytes(), "To Bytes");

            var optionFromBytes = new Options.Block1();

            optionFromBytes.FromBytes(expected);
            Assert.AreEqual(blockNumber, optionToBytes.BlockNumber, "Decoded Block Number from bytes");
            Assert.AreEqual(blockSize, optionToBytes.BlockSize, "Decoded Block Size from bytes");
            Assert.AreEqual(more, optionToBytes.IsMoreFollowing, "Decoded More following from bytes");
        }
    }
}
