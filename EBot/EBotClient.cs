using EBot.Commands;
using EBot.Logs;
using EBot.MemoryStream;
using EBot.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using Discord.Rest;
using EBot.MachineLearning;
using System.IO;

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
            this._Handler.RESTClient = this._DiscordREST;
            this._Handler.Log = this._Log;
            this._Handler.Prefix = this._Prefix;
            this._Handler.Source = this._Source;
            this._Handler.EmbedReply.Log = this._Log;
            this._Source.Initialize();

            this._Spy.Client = this._Discord;
            this._Spy.RESTClient = this._DiscordREST;
            this._Spy.Log = this._Log;

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

        public async Task InitializeAsync()
        {
            try
            {
                if(File.Exists("logs.txt"))
                {
                    File.Delete("logs.txt");
                }

                await this._Discord.LoginAsync(TokenType.Bot, _Token, true);
                await this._Discord.StartAsync();
                await this._DiscordREST.LoginAsync(TokenType.Bot, _Token, true);
                await LuaEnv.InitializeAsync(this);

                if (!this._HasInitialized)
                {
                    ClientMemoryStream.Initialize(this);
                    this._HasInitialized = true;
                }

                StreamingGame game = new StreamingGame(this._Prefix + "help | " + this._Prefix + "info",EBotConfig.TWITCH_URL);
                await this._Discord.SetActivityAsync(game);

                this._Discord.MessageDeleted += async (msg,chan) =>
                {
                    LuaEnv.OnMessageDeleted(msg, chan);
                };

                this._Discord.MessageReceived += async msg =>
                {
                    this._Spy.WatchWords(msg, new string[] { "yara", "earu" });
                    this._Handler.OnMessageCreated(msg);
                    LuaEnv.OnMessageReceived(msg);
                    Extras.InviteCheck(msg, this._Handler.EmbedReply);
                    Extras.Hentai(msg,this._Handler.EmbedReply);
                    Extras.Stylify(msg,this._Handler.EmbedReply);

                    ITextChannel chan = msg.Channel as ITextChannel;
                    if (!msg.Author.IsBot && !chan.IsNsfw && !msg.Content.StartsWith(this._Prefix))
                    {
                        ulong id = 0;
                        if(msg.Channel is IGuildChannel)
                        {
                            IGuildChannel guildchan = msg.Channel as IGuildChannel;
                            id = guildchan.Guild.Id;
                        }
                        MarkovHandler.Learn(msg.Content,id,this._Log);
                    }
                };

                this._Discord.ReactionAdded   += LuaEnv.OnReactionAdded;
                this._Discord.ReactionRemoved += LuaEnv.OnReactionRemoved;
                this._Discord.UserLeft        += LuaEnv.OnUserLeft;
                this._Discord.UserJoined      += LuaEnv.OnUserJoined;

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
