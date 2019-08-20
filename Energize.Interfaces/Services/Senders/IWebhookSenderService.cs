using Discord;
using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Senders
{
    public interface IWebhookSenderService : IServiceImplementation
    {
        Task<ulong> SendRawAsync(IMessage msg, string content, string userName, string avatarUrl);

        Task<ulong> SendRawAsync(ITextChannel chan, string content, string userName, string avatarUrl);

        Task<ulong> SendEmbedAsync(IMessage msg, Embed embed, string userName, string avatarUrl);

        Task<ulong> SendEmbedAsync(ITextChannel chan, Embed embed, string userName, string avatarUrl);

        Task<ulong> RepostMessageAsync(ITextChannel chan, IMessage msg, Embed embed = null);
    }
}
