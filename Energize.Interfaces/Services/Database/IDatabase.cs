using Energize.Interfaces.DatabaseModels;
using Energize.Interfaces.Services.Listeners;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Database
{
    public interface IDatabase
    {
        Task<IDiscordGuild> GetOrCreateGuildAsync(ulong id);

        Task<IDiscordUserStats> GetOrCreateUserStatsAsync(ulong id);

        Task<IDiscordUser> GetOrCreateUserAsync(ulong id);

        Task SaveYoutubeVideoIdsAsync(IEnumerable<IYoutubeVideoID> ytVideoIds);

        Task<IYoutubeVideoID> GetRandomVideoIdAsync();

        void Save();
    }
}
