using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBotsList.Api;
using DiscordBotsList.Api.Objects;
using Energize.Essentials;
using Energize.Essentials.Helpers;
using Energize.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private readonly string Token;
        private readonly AuthDiscordBotListApi DiscordBotList;
        private readonly Timer UpdateTimer;

        public EnergizeClient(string token, string prefix, char separator)
        {
            Console.Clear();
            Console.Title = "Energize's Logs";

            this.Token = token;
            this.Prefix = prefix;
            this.Separator = separator;
            this.Logger = new Logger();
            this.MessageSender = new MessageSender(this.Logger);
            this.DiscordClient = new DiscordShardedClient(new DiscordSocketConfig
            {
                MessageCacheSize = 100,
                ExclusiveBulkDelete = false,
                LogLevel = LogSeverity.Verbose,
            });
            this.DiscordRestClient = new DiscordRestClient();
            this.ServiceManager = new ServiceManager(this);
            this.UpdateTimer = new Timer(this.OnUpdateTimer);

            if (this.HasToken)
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    Exception e = (Exception)args.ExceptionObject;
                    this.Logger.LogTo("crash.log", e.ToString());
                };

                this.DiscordClient.Log += async log => this.Logger.LogTo("dnet_socket.log", log.Message);
                this.DiscordClient.ShardReady += this.OnShardReady;
                this.DiscordRestClient.Log += async log => this.Logger.LogTo("dnet_rest.log", log.Message);

                this.DiscordBotList = new AuthDiscordBotListApi(Config.Instance.Discord.BotID, Config.Instance.Discord.BotListToken);
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

        private volatile int CurrentShardCount;
        private async Task OnShardReady(DiscordSocketClient _)
        {
            if (this.DiscordClient.Shards.Count != ++this.CurrentShardCount) return;
            await this.ServiceManager.OnReadyAsync();
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

        public string Prefix { get; }
        public char Separator { get; }
        public DiscordShardedClient DiscordClient { get; }
        public DiscordRestClient DiscordRestClient { get; }
        public Logger Logger { get; }
        public MessageSender MessageSender { get; }
        public ServiceManager ServiceManager { get; }

        public string Environment => this.IsDevEnv ? "DEVELOPMENT" : "PRODUCTION"; 
        public bool HasToken => !string.IsNullOrWhiteSpace(this.Token); 

        private async Task<(bool, int)> UpdateBotWebsitesAsync()
        {
            int serverCount = this.DiscordClient.Guilds.Count;
            bool success = true;
            if (this.IsDevEnv) return (true, serverCount);
         
            try
            {
                var obj = new { guildCount = serverCount };
                if (JsonHelper.TrySerialize(obj, this.Logger, out string json))
                {
                    string endpoint = $"https://discord.bots.gg/api/v1/bots/{Config.Instance.Discord.BotID}/stats";
                    await HttpHelper.PostAsync(endpoint, json, this.Logger, null, req => {
                        req.Headers[System.Net.HttpRequestHeader.Authorization] = Config.Instance.Discord.BotsToken;
                        req.ContentType = "application/json";
                    });
                }

                IDblSelfBot me = await this.DiscordBotList.GetMeAsync();
                await me.UpdateStatsAsync(serverCount);
            }
            catch
            {
                success = false;
            }

            return (success, serverCount);
        }

        private async Task UpdateActivityAsync()
        {
            StreamingGame game = Config.Instance.Maintenance
                ? new StreamingGame("maintenance", Config.Instance.URIs.TwitchURL)
                : new StreamingGame($"{this.Prefix}help | {this.Prefix}info | {this.Prefix}docs",
                    Config.Instance.URIs.TwitchURL);
            await this.DiscordClient.SetActivityAsync(game);
        }

        private async Task NotifyCaughtExceptionsAsync()
        {
            RestChannel chan = await this.DiscordRestClient.GetChannelAsync(Config.Instance.Discord.BugReportChannelID);
            if (chan == null) return;

            IEnumerable<IGrouping<Exception, EventHandlerException>> exs = this.ServiceManager.TakeCaughtExceptions();
            foreach(IGrouping<Exception, EventHandlerException> grouping in exs)
            {
                EventHandlerException ex = grouping.FirstOrDefault();
                if (ex == null) continue;

                EmbedBuilder builder = new EmbedBuilder();
                builder
                    .WithField("Message", ex.Error.Message)
                    .WithField("File", ex.FileName)
                    .WithField("Method", ex.MethodName)
                    .WithField("Line", ex.Line)
                    .WithField("Occurences", grouping.Count())
                    .WithColorType(EmbedColorType.Warning)
                    .WithFooter("event handler error");

                await this.MessageSender.SendAsync(chan, builder.Build());
            }
        }

        private async void OnUpdateTimer(object _)
        {
            try
            {
                long mb = Process.GetCurrentProcess().WorkingSet64 / 1024L / 1024L; //b to mb
                GC.Collect();

                (bool success, int servercount) = await this.UpdateBotWebsitesAsync();
                string log = success
                    ? $"Collected {mb}MB of garbage, updated server count ({servercount})"
                    : $"Collected {mb}MB of garbage, did NOT update server count, API might be down";
                this.Logger.Nice("Update", ConsoleColor.Gray, log);

                await this.UpdateActivityAsync();
                await this.NotifyCaughtExceptionsAsync();
            }
            catch(Exception ex)
            {
                this.Logger.Danger(ex);
            }
        }

        private Task TryLoginAsync(Func<TokenType, string, bool, Task> loginFunc, TokenType tokenType, string token, int delay = 30, int times = 0)
            => loginFunc(tokenType, token, true).ContinueWith(async t =>
            {
                if (!t.IsFaulted) return;
                
                await Task.Delay(delay * 1000);

                if (times > 3)
                {
                    if (t.Exception?.InnerException != null)
                        this.Logger.Danger(t.Exception.InnerException);
                    else
                        this.Logger.Danger("Failed to login to discord");
                }
                else
                {
                    delay *= 2;
                    times += 1;
                    this.Logger.Warning($"Failed to login to discord, attempt {times}: {t.Exception}");
                    await this.TryLoginAsync(loginFunc, tokenType, token, delay, times);
                }
            });

        public async Task InitializeAsync()
        {
            if (!this.HasToken) return;

            try
            {
                await this.TryLoginAsync(this.DiscordClient.LoginAsync, TokenType.Bot, this.Token);
                await this.TryLoginAsync(this.DiscordRestClient.LoginAsync, TokenType.Bot, this.Token);
                if (this.DiscordClient.LoginState != LoginState.LoggedIn || this.DiscordRestClient.LoginState != LoginState.LoggedIn)
                    return;

                await this.DiscordClient.StartAsync();
                await this.UpdateActivityAsync();

                const int hour = 1000 * 60 * 60;
                this.UpdateTimer.Change(10000, hour);

                await this.ServiceManager.InitializeServicesAsync();
            }
            catch (Exception ex)
            {
                this.Logger.Nice("Init", ConsoleColor.Red, $"Something went wrong: {ex}");
            }
        }
    }
}