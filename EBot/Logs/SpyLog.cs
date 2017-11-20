using Discord;
using Discord.WebSocket;
using System;
using Discord.Rest;

namespace EBot.Logs
{
    public class SpyLog
    {
        private DiscordRestClient _RESTClient;
        private DiscordSocketClient _Client;
        private BotLog _Log;
        
        public DiscordSocketClient Client { get => this._Client; set => this._Client = value; }
        public BotLog Log { get => this._Log; set => this._Log = value; }
        public DiscordRestClient RESTClient { get => this._RESTClient; set => this._RESTClient = value; }

        private void LogMessage(SocketMessage msg)
        {
            string log = "";

            if (msg.Channel is IDMChannel)
            {
                IGuildChannel chan = msg.Channel as IGuildChannel;
                log += "(" + chan.Guild.Name + " - #" + msg.Channel.Name + ") ";
            }
            log += msg.Author.Username + "#" + msg.Author.Discriminator + " => [ " + msg.Content + " ]";

            this._Log.Nice("SpyLog", ConsoleColor.Yellow, log);
        }

        public void WatchWords(string[] tospy)
        {
            this._Client.MessageReceived += async msg =>
            {
                string content = msg.Content;
                for (int i = 0; i < tospy.Length; i++)
                {
                    string used = tospy[i];
                    if (content.ToLower().Contains(used))
                    {
                        this.LogMessage(msg);                        
                    }
                }
            };
        }
    }
}
