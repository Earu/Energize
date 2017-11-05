using DSharpPlus.Entities;
using EBot.Logs;
using EBot.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace EBot.Commands.Modules
{
    class UtilsCommands : ICommandModule
    {
        private string Name = "Util";
        private CommandsHandler Handler;
        private BotLog Log;


        public void Setup(CommandsHandler handler,BotLog log)
        {
            this.Handler = handler;
            this.Log = log;
        }

        private async Task Ping(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            DateTimeOffset createtimestamp = msg.CreationTimestamp;
            DateTimeOffset timestamp = msg.Timestamp;

            int diff = (createtimestamp.Millisecond - timestamp.Millisecond) / 10;


            await embedrep.Good(msg, "Pong!", ":alarm_clock: Discord: " + diff + "ms\n" +
                ":clock1: Bot: " + this.Handler.Client.Ping + "ms");
        }

        private async Task Help(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            string arg = args[0];
            if (!string.IsNullOrWhiteSpace(arg))
            {
                string desc = "";
                bool retrieved = this.Handler.CommandsHelp.TryGetValue(arg.ToLower().Trim(), out desc);
                if (retrieved)
                {
                    await embedrep.Warning(msg, "Help", "***" + arg + "*** : " + desc);
                }
                else
                {
                    await embedrep.Danger(msg, "Help", "Couldn't find documentation for \"" + arg + "\"");
                }
            }
            else
            {
                await embedrep.Good(msg, "Help", "Check your private messages " + Social.Action.PingUser(msg.Author));

                foreach (KeyValuePair<string, List<string>> module in this.Handler.ModuleCmds)
                {
                    string name = module.Key;
                    List<string> cmds = module.Value;
                    string result = "";

                    foreach(string cmd in cmds)
                    {
                        string help = this.Handler.CommandsHelp[cmd];
                        result += "**" + cmd + "**: " + help + "\n";
                    }

                    await embedrep.RespondByDM(msg, name, result, new DiscordColor(127, 255, 127));
                }
            }
        }

        private async Task Say(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            string tosay = string.Join(",", args.ToArray());
            await embedrep.Good(msg, msg.Author.Username, tosay);
        }

        private async Task Server(CommandReplyEmbed embedrep, DiscordMessage msg,List<string> args)
        {
            if (!msg.Channel.IsPrivate)
            {
                DiscordGuild guild = msg.Channel.Guild;

                string info = "";
                info += "**ID**: " + guild.Id + "\n";
                info += "**Owner**: " + guild.Owner.Username + "#" + guild.Owner.Discriminator + "\n";
                info += "**Members**: " + guild.MemberCount + "\n";
                info += "**Region**: " + guild.RegionId + "\n";
                info += "\n\n---- Emojis ----\n";

                int count = 0;
                foreach(DiscordEmoji emoji in guild.Emojis)
                {
                    info += "<:" + emoji.Name + ":" + emoji.Id + ">  ";
                    count++;
                    if(count >= 10)
                    {
                        info += "\n";
                        count = 0;
                    }
                }

                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                builder.WithThumbnailUrl(guild.IconUrl);
                builder.WithDescription(info);
                builder.WithTitle(guild.Name);
                builder.WithColor(new DiscordColor(110, 220, 110));

                await embedrep.Send(msg, builder.Build());
            }
            else
            {
                await embedrep.Danger(msg, "Hey!", "You can't do that in a DM channel!");
            }
        }

        private async Task Info(CommandReplyEmbed embedrep,DiscordMessage msg,List<string> args)
        {
            using (StreamReader reader = File.OpenText("External/info.json"))
            {
                string json = await reader.ReadToEndAsync();
                EBotAPI api = JSON.Deserialize<EBotAPI>(json, this.Log);

                string info = "";
                info += "**Name**: " + api.Name + "\n";
                info += "**Prefix**: " + api.Prefix + "\n";
                info += "**Commands**: " + api.CommandAmount + "\n";
                info += "**Servers**: " + api.GuildAmount + "\n";
                info += "**Users**: " + api.UserAmount + "\n";
                info += "**Owner**: " + api.Owner + "\n";
                info += "\n\n---- Invite link ----\n";
                info += "https://discordapp.com/oauth2/authorize?client_id=" + api.ID + "&scope=bot&permissions=0";

                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                builder.WithTitle("Info");
                builder.WithThumbnailUrl(api.Avatar);
                builder.WithDescription(info);
                builder.WithColor(new DiscordColor(110, 220, 110));

                await embedrep.Send(msg, builder.Build());
            }
        }

        public void Load()
        {
            this.Handler.LoadCommand("say", this.Say, "^say \"sentence\"",this.Name);
            this.Handler.LoadCommand("ping", this.Ping, "^ping",this.Name);
            this.Handler.LoadCommand("help", this.Help, "^help \"command|nothing\"",this.Name);
            this.Handler.LoadCommand("server", this.Server, "^server", this.Name);
            this.Handler.LoadCommand("info", this.Info, "^info", this.Name);

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload()
        {
            this.Handler.UnloadCommand("say");
            this.Handler.UnloadCommand("ping");
            this.Handler.UnloadCommand("help");

            this.Log.Nice("Module", ConsoleColor.Green, "Unloaded " + this.Name);
        }
    }
}
