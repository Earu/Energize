using System.Threading.Tasks;

namespace Energize.Interfaces.Services
{
    public interface IDatabaseService : IServiceImplementation
    {
        Task<IDatabaseContext> GetContext();
    }
}
