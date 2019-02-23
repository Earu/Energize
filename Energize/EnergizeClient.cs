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
            this.DiscordClient = new DiscordShardedClient(new DiscordSocketConfig
            {
                MessageCacheSize = 1000,
            });
            this.DiscordRestClient = new DiscordRestClient();
            this.ServiceManager = new ServiceManager(this);

            if (this.HasToken)
            {
                this.DisplayAsciiArt();
                bool isdevenv = this._Token == Config.TOKEN_DEV;
                this.Logger.Nice("Config", ConsoleColor.Yellow, $"Environment => [ {(isdevenv ? "DEVELOPMENT" : "PRODUCTION")} ]");
                this.Logger.Notify("Initializing");

                this.ServiceManager.InitializeServices();
            }
            else
            {
                this.Logger.Warning("No token was used! You NEED a token to connect to Discord!");
            }
        }

        private void DisplayAsciiArt()
        {
            ConsoleColor[] colors =
            {
                ConsoleColor.Blue, ConsoleColor.Cyan, ConsoleColor.Green,
                ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.Magenta,
            };
            string[] lines = StaticData.ASCII_ART.Split('\n');
            Random rand = new Random();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                ConsoleColor col = colors[i];
                Console.ForegroundColor = col;
                Console.WriteLine($"\t{line}");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n" + new string('-', 70));
        }

        public string               Prefix            { get; }
        public DiscordShardedClient DiscordClient     { get; }
        public DiscordRestClient    DiscordRestClient { get; }
        public Logger               Logger            { get; }
        public MessageSender        MessageSender     { get; }
        public ServiceManager       ServiceManager    { get; }

        public bool HasToken { get => !string.IsNullOrWhiteSpace(this._Token); }

        public async Task InitializeAsync()
        {
            if (!this.HasToken) return;

            try
            {
                if(File.Exists("logs.txt"))
                    File.Delete("logs.txt");

                await this.DiscordClient.LoginAsync(TokenType.Bot, _Token, true);
                await this.DiscordClient.StartAsync();
                await this.DiscordRestClient.LoginAsync(TokenType.Bot, _Token, true);
                await this.ServiceManager.InitializeServicesAsync(this);

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
