using System;
using System.Net;
using NUnit.Framework;
using System.Collections;

namespace CoAPNet.Udp.Tests
{
    [TestFixture]
    public class CoapUdpEndPointTests
    {
        public static IEnumerable CompareMessageIdTestCases
        {
            get
            {
                yield return new TestCaseData(
                        new CoapMessageIdentifier(1234, CoapMessageType.Confirmable, new byte[] { 1, 2, 3, 4 }, new CoapUdpEndPoint(IPAddress.Parse("1.2.3.4"), 1234), true),
                        new CoapMessageIdentifier(1234, CoapMessageType.Acknowledgement, new byte[] { 1, 2, 3, 4 }, new CoapUdpEndPoint(IPAddress.Parse("1.2.3.4"), 1234), false))
                    .Returns(true);

                yield return new TestCaseData(
                        new CoapMessageIdentifier(1234, CoapMessageType.Confirmable, new byte[] { 1, 2, 3, 4 }, new CoapUdpEndPoint(IPAddress.Parse("1.2.3.4"), 1234), true),
                        new CoapMessageIdentifier(5678, CoapMessageType.Confirmable, new byte[] { 1, 2, 3, 4 }, new CoapUdpEndPoint(IPAddress.Parse("1.2.3.4"), 1234), false))
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
