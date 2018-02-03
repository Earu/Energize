using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using Discord.Rest;
using System.IO;

namespace Energize
{
    public class EnergizeClient
    {
        private string _Prefix;
        private DiscordSocketClient _Discord;
        private DiscordRestClient _DiscordREST;
        private EnergizeLog _Log;
        private EnergizeMessage _MessageSender;
        private string _Token;

        public EnergizeClient(string token, string prefix)
        {
            Console.Clear();
            Console.Title = "Energize's Logs";

            this._Token = token;
            this._Prefix = prefix;
            this._Log = new EnergizeLog();
            this._MessageSender = new EnergizeMessage(this._Log);
            this._Discord = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 1000,
            });
            this._DiscordREST = new DiscordRestClient();

            this._Log.Nice("Config", ConsoleColor.Yellow, "Token used => [ " + token + " ]");
            this._Log.Notify("Initializing");

            Services.ServiceManager.LoadServices(this);
        }

        public string Prefix { get => this._Prefix; }
        public DiscordSocketClient Discord { get => this._Discord; }
        public DiscordRestClient DiscordREST { get => this._DiscordREST; }
        public EnergizeLog Log { get => this._Log; }
        public EnergizeMessage MessageSender { get => this._MessageSender; }

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
                await Services.ServiceManager.LoadServicesAsync(this);

                StreamingGame game = new StreamingGame(this._Prefix + "help | " + this._Prefix + "info",EnergizeConfig.TWITCH_URL);
                await this._Discord.SetActivityAsync(game);

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
                this._Log.Nice("Init", ConsoleColor.Red, $"Something went wrong: {e.Message}");
            }
        }
    }
}
