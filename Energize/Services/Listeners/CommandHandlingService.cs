using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.Services;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    [Service("Commands")]
    public class CommandHandlingService : ServiceImplementationBase
    {
        private readonly DiscordShardedClient _Client;
        private readonly DiscordRestClient _RestClient;
        private readonly Logger _Logger;
        private readonly MessageSender _MessageSender;
        private readonly IServiceManager _ServiceManager;

        public CommandHandlingService(EnergizeClient client)
        {
            this._Client = client.DiscordClient;
            this._RestClient = client.DiscordRestClient;
            this._Logger = client.Logger;
            this._MessageSender = client.MessageSender;
            this._ServiceManager = client.ServiceManager;
        }

        public Dictionary<string, Commands.Command.Command> RegisteredCommands { get => Commands.CommandHandler.GetRegisteredCommands(null); }

        public override void Initialize()
            => Commands.CommandHandler.Initialize(this._Client, this._RestClient, this._Logger, this._MessageSender, this._ServiceManager);

        public bool IsCommandMessage(IMessage msg)
        {
            var cmds = this.RegisteredCommands;
            foreach(KeyValuePair<string, Commands.Command.Command> kv in cmds)
            {
                if (msg.Content.StartsWith($"{Config.Instance.Discord.Prefix}{kv.Key}"))
                    return true;
            }

            return false;
        }

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
