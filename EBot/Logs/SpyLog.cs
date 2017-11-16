using DSharpPlus;
using DSharpPlus.Entities;
using System;

namespace EBot.Logs
{
    public class SpyLog
    {
        public DiscordClient _Client;
        public BotLog _Log;
        
        public DiscordClient Client { get => this._Client; set => this._Client = value; }
        public BotLog Log { get => this._Log; set => this._Log = value; }

        private void LogMessage(DiscordMessage msg)
        {
            string log = "";

            if (!msg.Channel.IsPrivate)
            {
                log += "(" + msg.Channel.Guild.Name + " - #" + msg.Channel.Name + ") ";
            }
            log += msg.Author.Username + "#" + msg.Author.Discriminator + " => [ " + msg.Content + " ]";

            this._Log.Nice("SpyLog", ConsoleColor.Yellow, log);
        }

        public void WatchWords(string[] tospy)
        {
            this._Client.MessageCreated += async e =>
            {
                string content = e.Message.Content;
                for (int i = 0; i < tospy.Length; i++)
                {
                    string used = tospy[i];
                    if (content.ToLower().Contains(used))
                    {
                        this.LogMessage(e.Message);                        
                    }
                }
            };
        }
    }
}
