using Energize.Interfaces.DatabaseModels;
using Energize.Interfaces.Services.Listeners;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Database
{
    public interface IDatabase
    {
        Task<IDiscordGuild> GetOrCreateGuild(ulong id);

        Task<IDiscordUserStats> GetOrCreateUserStats(ulong id);

        Task<IDiscordUser> GetOrCreateUser(ulong id);

        Task SaveYoutubeVideoIds(IEnumerable<IYoutubeVideoID> ytVideoIds);

        Task<IYoutubeVideoID> GetRandomVideoIdAsync();

        void Save();
    }
}
