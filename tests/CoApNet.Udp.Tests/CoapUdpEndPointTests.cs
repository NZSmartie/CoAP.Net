using System;
using System.Net;
using NUnit.Framework;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace CoAPNet.Udp.Tests
{
    [TestFixture]
    public class CoapUdpEndPointTests
    {
        /// <summary>
        /// Timout for any Tasks
        /// </summary>
        public readonly int MaxTaskTimeout = System.Diagnostics.Debugger.IsAttached ? -1 : 2000;

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

        [Test]
        public void CancelReceiveAsync()
        {
            // Arrange
            var endpoint = new CoapUdpEndPoint(IPAddress.Loopback);

            var safetyCt = new CancellationTokenSource(MaxTaskTimeout);
            var testCt = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Task receiveTask1;
            Task receiveTask2;
            Task receiveTask3;

            // Ack
            using (var client = new CoapClient(endpoint))
            {
                receiveTask1 = client.ReceiveAsync(testCt.Token);
                receiveTask2 = client.ReceiveAsync(testCt.Token);
                receiveTask3 = client.ReceiveAsync(testCt.Token);

                Task.Run(() =>
                {
                    // Assert
                    Assert.ThrowsAsync<TaskCanceledException>(
                        async () => await receiveTask1, $"{nameof(CoapClient.ReceiveAsync)} did not throw an {nameof(OperationCanceledException)} when the CancelationToken was canceled.");
                    Assert.ThrowsAsync<TaskCanceledException>(
                        async () => await receiveTask2, $"{nameof(CoapClient.ReceiveAsync)} did not throw an {nameof(OperationCanceledException)} when the CancelationToken was canceled.");
                    Assert.ThrowsAsync<TaskCanceledException>(
                        async () => await receiveTask3, $"{nameof(CoapClient.ReceiveAsync)} did not throw an {nameof(OperationCanceledException)} when the CancelationToken was canceled.");
                }, safetyCt.Token).Wait();
            }

            Assert.That(testCt.IsCancellationRequested, Is.True, "The test's CancellationToken should have timed out.");
            Assert.That(safetyCt.IsCancellationRequested, Is.False, "The test's safety CancellationToken timed out");
        }
    }
}
