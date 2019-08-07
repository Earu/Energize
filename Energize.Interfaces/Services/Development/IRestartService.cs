using Discord;
using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Development
{
    public interface IRestartService : IServiceImplementation
    {
        Task WarnChannelAsync(IChannel chan, string message);

        Task RestartAsync();
    }
}
