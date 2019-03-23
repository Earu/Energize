using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using Discord.Rest;
using System.IO;
using Energize.Essentials;
using Energize.Services;
using DiscordBotsList.Api;
using DiscordBotsList.Api.Objects;

namespace Energize
{
    public class EnergizeClient
    {
#if DEBUG
        private readonly bool IsDevEnv = true;
#else
        private readonly bool IsDevEnv = false;
#endif
        private readonly string _Token;
        private readonly AuthDiscordBotListApi _DiscordBotList;

        public EnergizeClient(string token, string prefix, char separator)
        {
            Console.Clear();
            Console.Title = "Energize's Logs";

            this._Token        = token;
            this.Prefix        = prefix;
            this.Separator     = separator;
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
                this._DiscordBotList = new AuthDiscordBotListApi(Config.Instance.Discord.BotID, Config.Instance.Discord.BotListToken);
                this.DisplayAsciiArt();

                this.Logger.Nice("Config", ConsoleColor.Yellow, $"Environment => [ {this.Environment} ]");
                this.Logger.Notify("Initializing");

                try
                {
                    this.ServiceManager.InitializeServices();
                }
                catch (Exception e)
                {
                    this.Logger.Nice("Init", ConsoleColor.Red, $"Something went wrong: {e}");
                }
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
            string[] lines = StaticData.Instance.AsciiArt.Split('\n');
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

        public string                Prefix            { get; }
        public char                  Separator         { get; }
        public DiscordShardedClient  DiscordClient     { get; }
        public DiscordRestClient     DiscordRestClient { get; }
        public Logger                Logger            { get; }
        public MessageSender         MessageSender     { get; }
        public ServiceManager        ServiceManager    { get; }

        public string Environment { get => this.IsDevEnv ? "DEVELOPMENT" : "PRODUCTION"; }
        public bool HasToken { get => !string.IsNullOrWhiteSpace(this._Token); }

        private async Task<int> UpdateDBLServerCount()
        {
            int servercount = this.DiscordClient.Guilds.Count;
            if (!this.IsDevEnv)
            {
                IDblSelfBot me = await this._DiscordBotList.GetMeAsync();
                await me.UpdateStatsAsync(servercount);
            }

            return servercount;
        }

        public async Task InitializeAsync()
        {
            if (!this.HasToken) return;

            try
            {
                if(File.Exists("logs.txt"))
                    File.Delete("logs.txt");

                await this.DiscordClient.LoginAsync(TokenType.Bot, this._Token, true);
                await this.DiscordClient.StartAsync();
                await this.DiscordRestClient.LoginAsync(TokenType.Bot, this._Token, true);
                await this.ServiceManager.InitializeServicesAsync(this);

                StreamingGame game = new StreamingGame($"{this.Prefix}help | {this.Prefix}info", Config.Instance.URIs.TwitchURL);
                await this.DiscordClient.SetActivityAsync(game);

                Timer updatetimer = new Timer(async arg =>
                {
                    long mb = GC.GetTotalMemory(true) / 1024 / 1024; //b to mb
                    GC.Collect();

                    int servercount = await this.UpdateDBLServerCount();
                    this.Logger.Nice("Update", ConsoleColor.Gray, $"Collected {mb}MB of garbage and updated server count ({servercount})");
                });

                int hour = 1000 * 60 * 60;
                updatetimer.Change(10000, hour);
            }
            catch (Exception e)
            {
                this.Logger.Nice("Init", ConsoleColor.Red, $"Something went wrong: {e}");
            }
        }
    }
}
