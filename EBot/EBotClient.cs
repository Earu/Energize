using EBot.Commands;
using EBot.Logs;
using EBot.MachineLearning;
using EBot.MemoryStream;
using EBot.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using Discord.Rest;

namespace EBot
{
    public class EBotClient
    {
        public static EBotClient CLIENT;

        private string _Prefix;
        private DiscordSocketClient _Discord;
        private DiscordRestClient _DiscordREST;
        private CommandHandler _Handler;
        private CommandSource _Source;
        private BotLog _Log;
        private LogEvent _Event;
        private SpyLog _Spy;
        private string _Token;
        private bool _HasInitialized;

        public EBotClient(string token,string prefix)
        {
            Console.Clear();
            Console.Title = "EBot's Logs";

            this._Token = token;
            this._Prefix = prefix;
            this._Log = new BotLog();
            this._Event = new LogEvent();
            this._Spy = new SpyLog();
            this._Handler = new CommandHandler();
            this._Source = new CommandSource(this._Handler,this._Log);
            this._Discord = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 1000,
            });

            this._DiscordREST = new DiscordRestClient();
            this._HasInitialized = false;

            this._Log.Nice("Config", ConsoleColor.Yellow, "Token used => [ " + token + " ]");
            this._Log.Notify("Initializing");

            this._Handler.Client = this._Discord;
            this._Handler._RESTClient = this._DiscordREST;
            this._Handler.Log = this._Log;
            this._Handler.Prefix = this._Prefix;
            this._Handler.Source = this._Source;
            this._Handler.EmbedReply.Log = this._Log;
            this._Handler.Initialize();

            this._Spy.Client = this._Discord;
            this._Spy.RESTClient = this._DiscordREST;
            this._Spy.Log = this._Log;
            this._Spy.WatchWords(new string[] { "yara", "earu" });

            this._Event.Client = this._Discord;
            this._Event.RESTClient = this._DiscordREST;
            this._Event.Prefix = this._Prefix;
            this._Event.Log = this._Log;
            this._Event.InitEvents();

            CLIENT = this;
        }

        public string Prefix { get => this._Prefix; set => this._Prefix = value; }
        public DiscordSocketClient Discord { get => this._Discord; set => this._Discord = value; }
        public DiscordRestClient DiscordREST { get => this._DiscordREST; set => this._DiscordREST = value; }
        public CommandHandler Handler { get => this._Handler; set => this._Handler = value; }
        public CommandSource Source { get => this._Source; set => this._Source = value; }
        public BotLog Log { get => this._Log; set => this._Log = value; }
        public LogEvent Event { get => this._Event; set => this._Event = value; }
        public SpyLog Spy { get => this._Spy; set => this._Spy = value; }

        private async Task OnChatAsync(SocketMessage msg)
        {
            if (!msg.Author.IsBot && !msg.Channel.IsNsfw)
            {
              MarkovHandler.Learn(msg.Content);
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                await this._Discord.LoginAsync(TokenType.Bot,_Token,true);
                await this._Discord.StartAsync();
                await this._DiscordREST.LoginAsync(TokenType.Bot, _Token, true);
                await LuaEnv.InitializeAsync(this);

                this._Discord.Ready += async () =>
                {
                    if (!this._HasInitialized)
                    {
                        ClientMemoryStream.Initialize(this);
                        this._HasInitialized = true;

                        Timer statustimer = new Timer(async arg =>
                        {
                            await this._Discord.SetStatusAsync(UserStatus.Offline);
                            await this._Discord.SetStatusAsync(UserStatus.Online);
                            //so status update properly

                            ClientInfo info = await ClientMemoryStream.GetClientInfo();
                            string gname = this._Prefix + "help w/ " + info.UserAmount + " users";
                            await this._Discord.SetGameAsync(gname, EBotConfig.TWITCH_URL, StreamType.Twitch);
                        });

                        statustimer.Change(0, 1000 * 60 * 5); //5mins
                    }
                };

                this._Discord.MessageReceived += this.OnChatAsync;

                Timer gctimer = new Timer(arg =>
                {
                    long mb = GC.GetTotalMemory(false)/1024/1024; //b to mb
                    GC.Collect();
                    this._Log.Nice("GC", ConsoleColor.Gray, "Collected " + mb + "MB of garbage");
                });

                int hour = 1000 * 60 * 60;
                gctimer.Change(hour, hour);
                
            }
            catch (Exception e)
            {
                this._Log.Nice("Init", ConsoleColor.Red, "Failed to connect");
                this._Log.Error(e.ToString());
            }
        }
    }
}
