using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CoAPNet.Tests
{
    [TestFixture]
    public class CoapStyleUriParserTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            CoapStyleUriParser.Register();
        }

        public static IEnumerable UriParseTestCases
        {
            get {
                yield return new TestCaseData("coap://localhost/", "coap://localhost/", "coap", 5683, true);
                yield return new TestCaseData("coap://localhost:5683/", "coap://localhost/", "coap", 5683, true);
                yield return new TestCaseData("coap://localhost:5684/", "coap://localhost:5684/", "coap", 5684, false);
                yield return new TestCaseData("coaps://localhost/", "coaps://localhost/", "coaps", 5684, true);
                yield return new TestCaseData("coaps://localhost:5684/", "coaps://localhost/", "coaps", 5684, true);
                yield return new TestCaseData("coaps://localhost:5684/", "coaps://localhost/", "coaps", 5684, true);
                yield return new TestCaseData("coaps://localhost:5683/", "coaps://localhost:5683/", "coaps", 5683, false);
            }
        }

        [TestCaseSource(nameof(UriParseTestCases))]
        public void UriParseTest(string uri, string toString, string scheme, int port, bool isDefaultPort)
        {
            var actual = new Uri(uri);

            Assert.That(actual.ToString(), Is.EqualTo(toString));
            Assert.That(actual.Scheme, Is.EqualTo(scheme));
            Assert.That(actual.Port, Is.EqualTo(port));
            Assert.That(actual.IsDefaultPort, Is.EqualTo(isDefaultPort));
        }

        public static IEnumerable UriCompareTestCases
        {
            get
            {
                yield return new TestCaseData("coap://localhost/", "coap://localhost/").Returns(0);
                yield return new TestCaseData("coap://localhost:5683/", "coap://localhost/").Returns(0);
                yield return new TestCaseData("coap://localhost:5683/", "coap://localhost/").Returns(0);
                yield return new TestCaseData("coaps://localhost/", "coaps://localhost/").Returns(0);
                yield return new TestCaseData("coaps://localhost:5684/", "coaps://localhost/").Returns(0);
                yield return new TestCaseData("https://localhost:443/", "https://localhost/").Returns(0);
            }
        }

        [TestCaseSource(nameof(UriCompareTestCases))]
        public int UriCompareTest(string first, string second)
        {
            var firstUri = new Uri(first);
            var secondUri = new Uri(second);

            return Uri.Compare(firstUri, secondUri, UriComponents.SchemeAndServer, UriFormat.SafeUnescaped, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
