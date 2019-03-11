using Discord;
using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Senders
{
    public interface IWebhookSenderService : IServiceImplementation
    {
        Task<ulong> SendRaw(IMessage msg, string content, string username, string avatarurl);

        Task<ulong> SendRaw(ITextChannel chan, string content, string username, string avatarurl);

        Task<ulong> SendEmbed(IMessage msg, Embed embed, string username, string avatarurl);

        Task<ulong> SendEmbed(ITextChannel chan, Embed embed, string username, string avatarurl);
    }
}
