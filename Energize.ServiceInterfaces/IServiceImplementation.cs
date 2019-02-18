using System.Threading.Tasks;

namespace Energize.Interfaces
{
    public interface IServiceImplementation
    {
        void Initialize();

        Task InitializeAsync();
    }
}
