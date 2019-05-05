using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBotsList.Api;
using DiscordBotsList.Api.Objects;
using Energize.Essentials;
using Energize.Services;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
                MessageCacheSize = 100,
            });
            this.DiscordRestClient = new DiscordRestClient();
            this.ServiceManager = new ServiceManager(this);

            if (this.HasToken)
            {
                AppDomain.CurrentDomain.UnhandledException += (sender,args) =>
                {
                    Exception e = (Exception)args.ExceptionObject;
                    this.Logger.LogTo("crash.log", e.ToString());
                };

                this.DiscordClient.Log += async log => this.Logger.LogTo("dnet_socket.log", log.Message);
                this.DiscordRestClient.Log += async log => this.Logger.LogTo("dnet_rest.log", log.Message);

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

        private async Task<(bool, int)> UpdateBotWebsites()
        {
            int servercount = this.DiscordClient.Guilds.Count;
            bool success = true;
            if (this.IsDevEnv) return (success, servercount);
         
            try
            {
                var obj = new { guildCount = servercount };
                string json = JsonPayload.Serialize(obj, this.Logger);
                string endpoint = $"https://discord.bots.gg/api/v1/bots/{Config.Instance.Discord.BotID}/stats";
                string resp = await HttpClient.PostAsync(endpoint, json, this.Logger, null, req => {
                    req.Headers[System.Net.HttpRequestHeader.Authorization] = Config.Instance.Discord.BotsToken;
                    req.ContentType = "application/json";
                });

                IDblSelfBot me = await this._DiscordBotList.GetMeAsync();
                await me.UpdateStatsAsync(servercount);
            }
            catch
            {
                success = false;
            }

            return (success, servercount);
        }

        private async Task UpdateActivity()
        {
            StreamingGame game = new StreamingGame($"{this.Prefix}help | {this.Prefix}info", Config.Instance.URIs.TwitchURL);
            await this.DiscordClient.SetActivityAsync(game);
        }

        public async Task InitializeAsync()
        {
            if (!this.HasToken) return;

            try
            {
                await this.DiscordClient.LoginAsync(TokenType.Bot, this._Token, true);
                await this.DiscordClient.StartAsync();
                await this.DiscordRestClient.LoginAsync(TokenType.Bot, this._Token, true);
                await this.UpdateActivity();

                Timer updatetimer = new Timer(async arg =>
                {
                    long mb = Process.GetCurrentProcess().WorkingSet64 / 1024L / 1024L; //b to mb
                    GC.Collect();

                    (bool success, int servercount) = await this.UpdateBotWebsites();
                    if (success)
                        this.Logger.Nice("Update", ConsoleColor.Gray, $"Collected {mb}MB of garbage, updated server count ({servercount})");
                    else
                        this.Logger.Nice("Update", ConsoleColor.Gray, $"Collected {mb}MB of garbage, did NOT update server count, API might be down");

                    await this.UpdateActivity();
                });

                int hour = 1000 * 60 * 60;
                updatetimer.Change(10000, hour);

                await this.ServiceManager.InitializeServicesAsync();
            }
            catch (Exception e)
            {
                this.Logger.Nice("Init", ConsoleColor.Red, $"Something went wrong: {e}");
            }
        }
    }
}