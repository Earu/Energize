using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Database
{
    public interface IDatabaseService : IServiceImplementation
    {
        Task<IDatabaseContext> GetContextAsync();

        IDatabaseContext CreateContext();
    }
}
