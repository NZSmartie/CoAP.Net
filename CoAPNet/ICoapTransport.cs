using System.Threading.Tasks;

namespace CoAPNet
{
    public interface ICoapTransportFactory
    {
        ICoapTransport Create(ICoapEndpoint endPoint, ICoapHandler handler);
    }

    public interface ICoapTransport
    {
        Task BindAsync();

        Task UnbindAsync();

        Task StopAsync();
    }
}