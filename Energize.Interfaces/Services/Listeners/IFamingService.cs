using Discord;
using Energize.Interfaces.DatabaseModels;
using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Listeners
{
    public interface IFamingService : IServiceImplementation
    {
        Task<bool> RemoveFameChannelAsync(IDiscordGuild dbGuild, IMessage msg);

        Task<ITextChannel> CreateAndSaveFameChannelAsync(IDiscordGuild dbGuild, IMessage msg);
    }
}
