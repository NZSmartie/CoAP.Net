using System.Threading.Tasks;

namespace CoAPNet
{
    public interface ICoapHandler
    {
        Task ProcessRequestAsync(ICoapConnectionInformation connection, byte[] payload);
    }
}