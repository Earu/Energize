using DSharpPlus;
using DSharpPlus.Entities;
using System;

namespace EBot.Logs
{
    public class LogEvent
    {
        private DiscordClient _Client;
        private string _Prefix;
        private BotLog _Log;
    
        public DiscordClient Client { get => this._Client; set => this._Client = value; }
        public string Prefix { get => this._Prefix; set => this._Prefix = value; }
        public BotLog Log { get => this._Log; set => this._Log = value; }

        public void InitEvents()
        {
            this._Client.Ready += async e =>
            {
                Console.WriteLine("\n\t---------\\\\\\\\ Done initializing ////---------\n");
            };

            this._Client.GuildAvailable += async e =>
            {
                this._Log.Nice("Guild",ConsoleColor.Magenta,"Online on " + e.Guild.Name + " || ID => [" + e.Guild.Id + "]");
            };

            this._Client.GuildUnavailable += async e =>
            {
                this._Log.Nice("Guild", ConsoleColor.Red, "Offline from " + e.Guild.Name + " || ID => [" + e.Guild.Id + "]");
            };

            this._Client.GuildCreated += async e =>
            {
                this._Log.Nice("Guild", ConsoleColor.Magenta, "Joined " + e.Guild.Name + " || ID => [" + e.Guild.Id + "]");
            };

            this._Client.GuildDeleted += async e =>
            {
                this._Log.Nice("Guild", ConsoleColor.Red, "Left " + e.Guild.Name + " || ID => [" + e.Guild.Id + "]");
            };

        }
    }
}
