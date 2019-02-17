using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using Discord.Rest;
using System.IO;
using Energize.Toolkit;
using Energize.Services;

namespace Energize
{
    public class EnergizeClient
    {
        private readonly string _Token;

        public EnergizeClient(string token, string prefix)
        {
            Console.Clear();
            Console.Title = "Energize's Logs";

            this._Token        = token;
            this.Prefix        = prefix;
            this.Logger        = new Logger();
            this.MessageSender = new MessageSender(this.Logger);
            this.DiscordClient       = new DiscordShardedClient(new DiscordSocketConfig
            {
                MessageCacheSize = 1000,
            });
            this.DiscordRestClient = new DiscordRestClient();
            this.ServiceManager = new ServiceManager();

            this.Logger.Nice("Config", ConsoleColor.Yellow, $"Token used => [ {token} ]");
            this.Logger.Notify("Initializing");

            ServiceManager.LoadServices(this);
        }

        public string               Prefix         { get; }
        public DiscordShardedClient DiscordClient        { get; }
        public DiscordRestClient    DiscordRestClient    { get; }
        public Logger               Logger         { get; }
        public MessageSender        MessageSender  { get; }
        public ServiceManager       ServiceManager { get; }

        public async Task InitializeAsync()
        {
            try
            {
                if(File.Exists("logs.txt"))
                    File.Delete("logs.txt");

                await this.DiscordClient.LoginAsync(TokenType.Bot, _Token, true);
                await this.DiscordClient.StartAsync();
                await this.DiscordRestClient.LoginAsync(TokenType.Bot, _Token, true);
                await ServiceManager.LoadServicesAsync(this);

                StreamingGame game = new StreamingGame($"{this.Prefix}help | {this.Prefix}info",Config.TWITCH_URL);
                await this.DiscordClient.SetActivityAsync(game);

                Timer gctimer = new Timer(arg =>
                {
                    long mb = GC.GetTotalMemory(false) / 1024 / 1024; //b to mb
                    GC.Collect();
                    this.Logger.Nice("GC", ConsoleColor.Gray, $"Collected {mb}MB of garbage");
                });

                int hour = 1000 * 60 * 60;
                gctimer.Change(hour, hour);

            }
            catch (Exception e)
            {
                this.Logger.Nice("Init", ConsoleColor.Red, $"Something went wrong: {e.Message}");
            }
        }
    }
}
