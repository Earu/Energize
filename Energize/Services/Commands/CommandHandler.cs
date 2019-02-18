using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Interfaces;
using Energize.Toolkit;
using System.Threading.Tasks;

namespace Energize.Services.Commands
{
    [Service("Commands")]
    public class CommandHandler : IServiceImplementation
    {
        private readonly DiscordShardedClient _Client;
        private readonly DiscordRestClient _RestClient;
        private readonly Logger _Logger;
        private readonly MessageSender _MessageSender;
        private readonly string _Prefix;
        private readonly IServiceManager _ServiceManager;

        public CommandHandler(EnergizeClient client)
        {
            this._Client = client.DiscordClient;
            this._RestClient = client.DiscordRestClient;
            this._Logger = client.Logger;
            this._MessageSender = client.MessageSender;
            this._Prefix = client.Prefix;
            this._ServiceManager = client.ServiceManager;
        }

        public void Initialize()
            => Energize.Commands.CommandHandler.initialize(this._Client, this._RestClient, this._Logger, 
                this._MessageSender, this._Prefix, this._ServiceManager);

        public Task InitializeAsync()
            => Task.CompletedTask;

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
            => Energize.Commands.CommandHandler.handleMessageReceived(msg);

        [Event("MessageDeleted")]
        public async Task MessageDeleted(Cacheable<IMessage,ulong> cache,ISocketMessageChannel chan)
            => this._Logger.Warning(cache.ToString() + chan.ToString());
    }
}
