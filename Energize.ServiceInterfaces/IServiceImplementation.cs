using System.Threading.Tasks;

namespace Energize.ServiceInterfaces
{
    public interface IServiceImplementation
    {
        void Initialize();

        Task InitializeAsync();
    }
}
