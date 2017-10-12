using DSharpPlus;
using DSharpPlus.Entities;
using EBot.Logs;
using System;
using System.Collections.Generic;

namespace EBot.Commands
{
    class CommandReplyEmbed
    {

        private BotLog _Log;

        public BotLog Log { get => this._Log; set => this._Log = value; }

        public async void Send(DiscordMessage msg,string header="",string content="",DiscordColor color=new DiscordColor())
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Color = color;
            builder.Title = header;
            builder.Description = content;

            try
            {
                await msg.RespondAsync(null, false, builder.Build());
            }
            catch
            {
                builder.Color = new DiscordColor(220, 110, 110);
                builder.Title = "Uh?!";
                builder.Description = "Looks like something went wrong!";
                await msg.RespondAsync(null, false, builder.Build());
                _Log.Nice("EmbedReply", ConsoleColor.Red, "Couldn't reply to => [ " + msg.Author.Username + ": " + msg.Content + " ]");
            }

        }

        public async void Send(DiscordChannel chan,string header="",string content="",DiscordColor color=new DiscordColor())
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Color = color;
            builder.Title = header;
            builder.Description = content;

            try
            {
                await chan.SendMessageAsync(null, false, builder.Build());
            }
            catch
            {
                builder.Color = new DiscordColor(220, 110, 110);
                builder.Title = "Uh?!";
                builder.Description = "Looks like something went wrong!";
                await chan.SendMessageAsync(null, false, builder.Build());
                this._Log.Nice("EmbedReply", ConsoleColor.Red, "Couldn't send message to => [ " + chan.Guild.Name + " " + chan.Name +" ]");
            }
        }

        public async void Send(DiscordMessage msg,DiscordEmbed embed = null)
        {
            await msg.RespondAsync(null, false, embed);
        }


        public async void Normal(DiscordMessage msg,string header,string content)
        {
            this.Send(msg, header, content, new DiscordColor(175, 175, 175));
        }

        public async void Warning(DiscordMessage msg,string header,string content)
        {
            this.Send(msg, header, content, new DiscordColor(220,180,80));
        }

        public async void Danger(DiscordMessage msg, string header, string content)
        {
            this.Send(msg, header, content, new DiscordColor(220, 110, 110));
        }

        public async void Good(DiscordMessage msg, string header, string content)
        {
            this.Send(msg, header, content, new DiscordColor(110, 220, 110));
        }

        public async void Disconnect(DiscordClient client)
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
