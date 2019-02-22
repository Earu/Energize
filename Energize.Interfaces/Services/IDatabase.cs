using Energize.Interfaces.DatabaseModels;
using System.Threading.Tasks;

namespace Energize.Interfaces.Services
{
    public interface IDatabase
    {
        Task<IDiscordGuild> GetOrCreateGuild(ulong id);

        Task<IDiscordUserStats> GetOrCreateUserStats(ulong id);

        Task<IDiscordUser> GetOrCreateUser(ulong id);

        void Save();
    }
}
