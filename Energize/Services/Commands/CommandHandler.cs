using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Toolkit;
using System.Threading.Tasks;

namespace Energize.Services.Commands
{
    [Service("Commands")]
    public class CommandHandler
    {
        private readonly DiscordShardedClient _Client;
        private readonly DiscordRestClient _RestClient;
        private readonly Logger _Logger;
        private readonly MessageSender _MessageSender;
        private readonly string _Prefix;

        public CommandHandler(EnergizeClient client)
        {
            this._Client = client.Discord;
            this._RestClient = client.DiscordREST;
            this._Logger = client.Logger;
            this._MessageSender = client.MessageSender;
            this._Prefix = client.Prefix;
        }

        /*
         * Service methods 
         * Do not change the signature of those functions
         */
        public void Initialize()
            => Energize.Commands.CommandHandler.initialize(this._Client, this._RestClient, this._Logger, this._MessageSender, this._Prefix);

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
            => Energize.Commands.CommandHandler.handleMessageReceived(msg);

        [Event("MessageDeleted")]
        public async Task MessageDeleted(Cacheable<IMessage,ulong> cache,ISocketMessageChannel chan)
        {
            this._Logger.Warning(cache.ToString() + chan.ToString());
        }
    }
}
