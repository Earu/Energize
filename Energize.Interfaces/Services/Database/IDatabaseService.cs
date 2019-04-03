using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Database
{
    public interface IDatabaseService : IServiceImplementation
    {
        Task<IDatabaseContext> GetContext();

        IDatabaseContext CreateContext();
    }
}
