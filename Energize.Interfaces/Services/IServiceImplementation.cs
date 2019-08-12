using System.Threading.Tasks;

namespace Energize.Interfaces.Services
{
    public interface IServiceImplementation
    {
        void Initialize();

        Task InitializeAsync();

        Task OnReadyAsync();
    }
}
