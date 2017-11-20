using Discord;
using Discord.WebSocket;
using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EBot.Commands
{
    public class CommandReplyEmbed
    {

        private BotLog _Log;
        private CommandsHandler _Handler;

        public BotLog Log { get => this._Log; set => this._Log = value; }
        public CommandsHandler Handler { get => this._Handler; set => this._Handler = value; }

        public async Task Send(SocketMessage msg,string header="",string content="",Color color=new Color())
        {
            string username = msg.Author.Username;
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(color);
            builder.WithDescription(content);
            builder.WithFooter(header);
            builder.WithAuthor(msg.Author);

            try
            {
                if (!string.IsNullOrWhiteSpace(content))
                {
                    await msg.Channel.SendMessageAsync("", false, builder.Build());
                }
            }
            catch
            {
                _Log.Nice("EmbedReply", ConsoleColor.Red, "Couldn't reply to => [ " + msg.Author.Username + ": " + msg.Content + " ]");
            }

        }

        public async Task Send(ISocketMessageChannel chan,string header="",string content="",Color color=new Color())
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(color);
            builder.WithDescription(content);
            builder.WithFooter(header);

            try
            {
                if (!string.IsNullOrWhiteSpace(content))
                {
                    await chan.SendMessageAsync("", false, builder.Build());
                }
            }
            catch
            {
                string guild = "";
                if(chan is IGuildChannel)
                {
                    IGuildChannel c = chan as IGuildChannel;
                    guild = c.Guild.Name + " ";
                }
                this._Log.Nice("EmbedReply", ConsoleColor.Red, "Couldn't send message to => [ " + guild + chan.Name +" ]");
            }
        }

        public async Task Send(SocketMessage msg,Embed embed = null)
        {
            try
            { 
                await msg.Channel.SendMessageAsync("", false, embed);
            }
            catch
            {
                string guild = "";
                if (!(msg.Channel is IDMChannel))
                {
                    IGuildChannel c = msg.Channel as IGuildChannel;
                    guild = c.Guild.Name;
                }
                this._Log.Nice("EmbedReply", ConsoleColor.Red, "Couldn't send message to => [ " + guild + " " + msg.Channel.Name +" ]");
            }
        }

        public async Task RespondByDM(SocketMessage msg,string header="",string content="",Color color=new Color())
        {
            try
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(color);
                builder.WithDescription(content);
                builder.WithFooter(header);
                builder.WithAuthor(msg.Author);

                IDMChannel chan = await msg.Author.GetOrCreateDMChannelAsync();
                await chan.SendMessageAsync("",false,builder.Build());
            }
            catch(Exception e)
            {
                this._Log.Danger(e.ToString());
                this._Log.Nice("DM", ConsoleColor.Red, "Couldn't send a DM to " + msg.Author.Username + "#" + msg.Author.Discriminator);
            }
        }

        public async Task Normal(SocketMessage msg,string header,string content)
        {
            await this.Send(msg, header, content, new Color(175, 175, 175));
        }

        public async Task Warning(SocketMessage msg,string header,string content)
        {
            await this.Send(msg, header, content, new Color(220,180,80));
        }

        public async Task Danger(SocketMessage msg, string header, string content)
        {
            await this.Send(msg, header, content, new Color(220, 110, 110));
        }

        public async Task Good(SocketMessage msg, string header, string content)
        {
            //110, 220, 110
            await this.Send(msg, header, content, new Color());
        }

        public async Task Disconnect(DiscordSocketClient client)
        {
            await client.LogoutAsync();
        }
    }
}
