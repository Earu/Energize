using System;
using Discord.Rest;
using Discord.WebSocket;

namespace EBot.Logs
{
    public class LogEvent
    {
        private DiscordRestClient _RESTClient;
        private DiscordSocketClient _Client;
        private string _Prefix;
        private BotLog _Log;
        
        public DiscordRestClient RESTClient { get => this._RESTClient; set => this._RESTClient = value; }
        public DiscordSocketClient Client { get => this._Client; set => this._Client = value; }
        public string Prefix { get => this._Prefix; set => this._Prefix = value; }
        public BotLog Log { get => this._Log; set => this._Log = value; }

        public void InitEvents()
        {
            this._Client.Connected += async () =>
            {
                this._Log.Notify("Ready");
            };

            this._Client.GuildAvailable += async guild =>
            {
                this._Log.Nice("Guild",ConsoleColor.Magenta,"Online on " + guild.Name + " || ID => [ " + guild.Id + " ]");
            };

            this._Client.GuildUnavailable += async guild =>
            {
                this._Log.Nice("Guild", ConsoleColor.Red, "Offline from " + guild.Name + " || ID => [ " + guild.Id + " ]");
            };

            this._Client.JoinedGuild += async guild =>
            {
                this._Log.Nice("Guild", ConsoleColor.Magenta, "Joined " + guild.Name + " || ID => [ " + guild.Id + " ]");
            };

            this._Client.LeftGuild += async guild =>
            {
                this._Log.Nice("Guild", ConsoleColor.Red, "Left " + guild.Name + " || ID => [ " + guild.Id + " ]");
            };

        }
    }
}
