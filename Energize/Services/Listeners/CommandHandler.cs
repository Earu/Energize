using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Interfaces.Services;
using Energize.Essentials;
using System.IO;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
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
            => Commands.CommandHandler.Initialize(this._Client, this._RestClient, this._Logger, 
                this._MessageSender, this._Prefix, this._ServiceManager);

        public Task InitializeAsync()
            => Task.CompletedTask;

        [Event("ShardReady")]
        public async Task OnShardReady(DiscordSocketClient _)
        {
            if (!File.Exists("restartlog.txt")) return;

            string content = File.ReadAllText("restartlog.txt");
            if (ulong.TryParse(content, out ulong id))
            {
                SocketChannel chan = this._Client.GetChannel(id);
                if (chan != null)
                    await this._MessageSender.Good(chan, "Restart", "Done restarting.");
            }

            File.Delete("restartlog.txt");
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
            => Commands.CommandHandler.HandleMessageReceived(msg);

        [Event("MessageDeleted")]
        public async Task OnMessageDeleted(Cacheable<IMessage, ulong> cache, ISocketMessageChannel chan)
            => Commands.CommandHandler.HandleMessageDeleted(cache, chan);

        [Event("MessageUpdated")]
        public async Task OnMessageUpdated(Cacheable<IMessage, ulong> cache, SocketMessage msg, ISocketMessageChannel chan)
            => Commands.CommandHandler.HandleMessageUpdated(cache, msg, chan);
    }
}
