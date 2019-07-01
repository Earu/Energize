using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.Services;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    [Service("Commands")]
    public class CommandHandlingService : ServiceImplementationBase
    {
        private readonly DiscordShardedClient Client;
        private readonly DiscordRestClient RestClient;
        private readonly Logger Logger;
        private readonly MessageSender MessageSender;
        private readonly IServiceManager ServiceManager;

        public CommandHandlingService(EnergizeClient client)
        {
            this.Client = client.DiscordClient;
            this.RestClient = client.DiscordRestClient;
            this.Logger = client.Logger;
            this.MessageSender = client.MessageSender;
            this.ServiceManager = client.ServiceManager;
        }

        public Dictionary<string, Commands.Command.Command> RegisteredCommands => Commands.CommandHandler.GetRegisteredCommands(null); 

        public override void Initialize()
            => Commands.CommandHandler.Initialize(this.Client, this.RestClient, this.Logger, this.MessageSender, this.ServiceManager);

        public bool IsCommandMessage(IMessage msg)
        {
            if (string.IsNullOrWhiteSpace(msg.Content))
                return false;

            Dictionary<string, Commands.Command.Command> cmds = this.RegisteredCommands;
            foreach(KeyValuePair<string, Commands.Command.Command> kv in cmds)
            {
                if (msg.Content.StartsWith($"{Config.Instance.Discord.Prefix}{kv.Key}"))
                    return true;

                if (Regex.IsMatch(msg.Content, $"^<@!?{Config.Instance.Discord.BotID}> {kv.Key}"))
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
                SocketChannel chan = this.Client.GetChannel(id);
                if (chan != null)
                    await this.MessageSender.Good(chan, "Restart", "Done restarting.");
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
        {
            IMessage oldMsg = await cache.GetOrDownloadAsync();
            if (oldMsg != null && !oldMsg.Content.Equals(msg.Content))
                Commands.CommandHandler.HandleMessageUpdated(cache, msg, chan);
        }
    }
}
