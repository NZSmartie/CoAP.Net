using Moq;

namespace CoAPNet.Tests.Mocks
{
    public class MockConnectionInformation : ICoapConnectionInformation
    {
        public MockConnectionInformation(ICoapEndpoint endPoint)
        {
            LocalEndpoint = endPoint;
        }

        public ICoapEndpoint LocalEndpoint { get; }

        public ICoapEndpoint RemoteEndpoint => new Mock<ICoapEndpoint>().Object;
    }
}