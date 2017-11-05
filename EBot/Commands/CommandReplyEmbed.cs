using DSharpPlus;
using DSharpPlus.Entities;
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

        public async Task Send(DiscordMessage msg,string header="",string content="",DiscordColor color=new DiscordColor())
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Color = color;
            builder.Title = header;
            builder.Description = content;

            try
            {
                if (!string.IsNullOrWhiteSpace(content))
                {
                    await msg.RespondAsync(null, false, builder.Build());
                }
            }
            catch
            {
                _Log.Nice("EmbedReply", ConsoleColor.Red, "Couldn't reply to => [ " + msg.Author.Username + ": " + msg.Content + " ]");
            }

        }

        public async Task Send(DiscordChannel chan,string header="",string content="",DiscordColor color=new DiscordColor())
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Color = color;
            builder.Title = header;
            builder.Description = content;

            try
            {
                if (!string.IsNullOrWhiteSpace(content))
                {
                    await chan.SendMessageAsync(null, false, builder.Build());
                }
            }
            catch
            {
                this._Log.Nice("EmbedReply", ConsoleColor.Red, "Couldn't send message to => [ " + chan.Guild.Name + " " + chan.Name +" ]");
            }
        }

        public async Task Send(DiscordMessage msg,DiscordEmbed embed = null)
        {
            try
            { 
                await msg.RespondAsync(null, false, embed);
            }
            catch
            {
                this._Log.Nice("EmbedReply", ConsoleColor.Red, "Couldn't send message to => [ " + msg.Channel.Guild.Name + " " + msg.Channel.Name +" ]");
            }
        }

        public async Task RespondByDM(DiscordMessage msg,string header="",string content="",DiscordColor color=new DiscordColor())
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            try
            {
                DiscordDmChannel chan = await this._Handler.Client.CreateDmAsync(msg.Author);
                await Send(chan,header,content,color);
            }
            catch
            {
                this._Log.Nice("DM", ConsoleColor.Red, "Couldn't send a DM to " + msg.Author.Username + "#" + msg.Author.Discriminator);
            }
        }

        public async Task Normal(DiscordMessage msg,string header,string content)
        {
            await this.Send(msg, header, content, new DiscordColor(175, 175, 175));
        }

        public async Task Warning(DiscordMessage msg,string header,string content)
        {
            await this.Send(msg, header, content, new DiscordColor(220,180,80));
        }

        public async Task Danger(DiscordMessage msg, string header, string content)
        {
            await this.Send(msg, header, content, new DiscordColor(220, 110, 110));
        }

        public async Task Good(DiscordMessage msg, string header, string content)
        {
            //110, 220, 110
            await this.Send(msg, header, content, new DiscordColor());
        }

        public async Task Disconnect(DiscordClient client)
        {
            IReadOnlyDictionary<ulong, DiscordGuild> guilds = client.Guilds;
            foreach (KeyValuePair<ulong, DiscordGuild> guild in guilds)
            {
                DiscordChannel chan = guild.Value.GetDefaultChannel();
                //Send(chan, "Poof!", "Whoops seems like I have to go!", new DiscordColor(220, 110, 110));
            }

            await client.DisconnectAsync();
        }
    }
}
