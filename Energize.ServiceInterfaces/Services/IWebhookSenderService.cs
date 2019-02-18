using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Energize.Interfaces.Services
{
    public interface IWebhookSenderService : IServiceImplementation
    {
        Task<ulong> SendRaw(SocketMessage msg, string content, string username, string avatarurl);

        Task<ulong> SendRaw(ITextChannel chan, string content, string username, string avatarurl);

        Task<ulong> SendEmbed(SocketMessage msg, Embed embed, string username, string avatarurl);

        Task<ulong> SendEmbed(ITextChannel chan, Embed embed, string username, string avatarurl);
    }
}
