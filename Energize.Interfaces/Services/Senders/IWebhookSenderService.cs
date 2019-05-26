using Discord;
using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Senders
{
    public interface IWebhookSenderService : IServiceImplementation
    {
        Task<ulong> SendRaw(IMessage msg, string content, string userName, string avatarUrl);

        Task<ulong> SendRaw(ITextChannel chan, string content, string userName, string avatarUrl);

        Task<ulong> SendEmbed(IMessage msg, Embed embed, string userName, string avatarUrl);

        Task<ulong> SendEmbed(ITextChannel chan, Embed embed, string userName, string avatarUrl);
    }
}
