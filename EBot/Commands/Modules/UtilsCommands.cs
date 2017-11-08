using DSharpPlus.Entities;
using EBot.Logs;
using EBot.MemoryStream;
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
                bool retrieved = this.Handler.CommandsHelp.TryGetValue(arg.ToLower().Trim(), out string desc);
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
                await embedrep.Good(msg, "Help", "Check your public messages " + msg.Author.Mention);

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

                    await embedrep.RespondByDM(msg, name, result, new DiscordColor());
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

                if (guild.Emojis.Count > 0)
                {
                    info += "\n\n---- Emojis ----\n";

                    int count = 0;
                    foreach (DiscordEmoji emoji in guild.Emojis)
                    {
                        info += "<:" + emoji.Name + ":" + emoji.Id + ">  ";
                        count++;
                        if (count >= 10)
                        {
                            info += "\n";
                            count = 0;
                        }
                    }
                }

                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                builder.WithThumbnailUrl(guild.IconUrl);
                builder.WithDescription(info);
                builder.WithTitle(guild.Name);
                builder.WithColor(new DiscordColor());

                await embedrep.Send(msg, builder.Build());
            }
            else
            {
                await embedrep.Danger(msg, "Hey!", "You can't do that in a DM channel!");
            }
        }

        private async Task Info(CommandReplyEmbed embedrep,DiscordMessage msg,List<string> args)
        {
            EBotInfo info = await EBotMemoryStream.GetClientInfo();

            string desc = "";
            desc += "**Name**: " + info.Name + "\n";
            desc += "**Prefix**: " + info.Prefix + "\n";
            desc += "**Commands**: " + info.CommandAmount + "\n";
            desc += "**Servers**: " + info.GuildAmount + "\n";
            desc += "**Users**: " + info.UserAmount + "\n";
            desc += "**Owner**: " + info.Owner + "\n";

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.WithTitle("Info");
            builder.WithThumbnailUrl(info.Avatar);
            builder.WithDescription(desc);
            builder.WithColor(new DiscordColor());

            await embedrep.Send(msg, builder.Build());
        }

        private async Task Invite(CommandReplyEmbed embedrep,DiscordMessage msg,List<string> args)
        {
            string invite = "https://discordapp.com/oauth2/authorize?client_id=" + EBotCredentials.BOT_ID_MAIN + "&scope=bot&permissions=0";
            await embedrep.Good(msg, msg.Author.Username, invite);
        }

        public void Load()
        {
            this.Handler.LoadCommand("say", this.Say, "^say \"sentence\"", this.Name);
            this.Handler.LoadCommand("ping", this.Ping, "^ping", this.Name);
            this.Handler.LoadCommand("help", this.Help, "^help \"command|nothing\"", this.Name);
            this.Handler.LoadCommand("server", this.Server, "^server", this.Name);
            this.Handler.LoadCommand("info", this.Info, "^info", this.Name);
            this.Handler.LoadCommand("invite", this.Invite, "^invite", this.Name);

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload()
        {
            this.Handler.UnloadCommand("say");
            this.Handler.UnloadCommand("ping");
            this.Handler.UnloadCommand("help");
            this.Handler.UnloadCommand("server");
            this.Handler.UnloadCommand("info");
            this.Handler.UnloadCommand("invite");

            this.Log.Nice("Module", ConsoleColor.Green, "Unloaded " + this.Name);
        }
    }
}
