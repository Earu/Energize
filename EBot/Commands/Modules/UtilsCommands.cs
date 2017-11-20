using Discord;
using Discord.Rest;
using Discord.WebSocket;
using EBot.Logs;
using EBot.MemoryStream;
using EBot.Utils;
using System;
using System.Collections.Generic;
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

        private async Task Ping(CommandReplyEmbed embedrep, SocketMessage msg, List<string> args)
        {
            DateTimeOffset createtimestamp = msg.CreatedAt;
            DateTimeOffset timestamp = msg.Timestamp;

            int diff = timestamp.Millisecond / 10;


            await embedrep.Good(msg, "Pong!", ":alarm_clock: Discord: " + diff + "ms\n" +
                ":clock1: Bot: " + this.Handler.Client.Latency + "ms");
        }

        private async Task Help(CommandReplyEmbed embedrep, SocketMessage msg, List<string> args)
        {
            string arg = args[0];
            if (!string.IsNullOrWhiteSpace(arg))
            {
                bool retrieved = this.Handler.Commands.TryGetValue(arg.ToLower().Trim(), out Command cmd);
                if (retrieved)
                {
                    await embedrep.Warning(msg, "Help [ " + arg + " ]", cmd.GetHelp());
                }
                else
                {
                    await embedrep.Danger(msg, "Help", "Couldn't find documentation for \"" + arg + "\"");
                }
            }
            else
            {
                if (!(msg.Channel is IDMChannel))
                {
                    await embedrep.Good(msg, "Help", "Check your private messages " + msg.Author.Mention);
                }

                Dictionary<string,Command> cmds = this.Handler.Commands;
                string result = "``";
                uint count = 0;
                foreach (KeyValuePair<string,Command> cmd in cmds)
                {
                    result += cmd.Key + ",";
                    count++;
                    if(count > 5)
                    {
                        result += "\n";
                        count = 0;
                    }
                }
                result = result.Remove(result.Length - 2);
                result += "``";

                await embedrep.RespondByDM(msg, "Help [ all ]", result, new Color());
            }
        }

        private async Task Say(CommandReplyEmbed embedrep, SocketMessage msg, List<string> args)
        {
            string tosay = string.Join(",", args.ToArray());
            await embedrep.Good(msg,"Say", tosay);
        }

        private async Task Server(CommandReplyEmbed embedrep, SocketMessage msg,List<string> args)
        {
            if (msg.Channel is IGuildChannel)
            {
                SocketGuild guild = (msg.Channel as IGuildChannel).Guild as SocketGuild;
                RestUser owner = await this.Handler.RESTClient.GetUserAsync(guild.OwnerId);

                string info = "";
                info += "**ID**: " + guild.Id + "\n";
                info += "**Owner**: " + (owner == null ? "NULL\n" : owner.Username + "#" + owner.Discriminator + "\n");
                info += "**Members**: " + guild.MemberCount + "\n";
                info += "**Region**: " + guild.VoiceRegionId + "\n";

                if (guild.Emotes.Count > 0)
                {
                    info += "\n\n---- Emojis ----\n";

                    int count = 0;
                    foreach (Emote emoji in guild.Emotes)
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

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithThumbnailUrl(guild.IconUrl);
                builder.WithDescription(info);
                builder.WithFooter(guild.Name);
                builder.WithColor(new Color());
                builder.WithAuthor(msg.Author);

                await embedrep.Send(msg, builder.Build());
            }
            else
            {
                await embedrep.Danger(msg, "Hey!", "You can't do that in a DM channel!");
            }
        }

        private async Task Info(CommandReplyEmbed embedrep,SocketMessage msg,List<string> args)
        {
            ClientInfo info = await ClientMemoryStream.GetClientInfo();

            string desc = "";
            desc += "**Name**: " + info.Name + "\n";
            desc += "**Prefix**: " + info.Prefix + "\n";
            desc += "**Commands**: " + info.CommandAmount + "\n";
            desc += "**Servers**: " + info.GuildAmount + "\n";
            desc += "**Users**: " + info.UserAmount + "\n";
            desc += "**Owner**: " + info.Owner + "\n";

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithFooter("Info");
            builder.WithThumbnailUrl(info.Avatar);
            builder.WithDescription(desc);
            builder.WithColor(new Color());
            builder.WithAuthor(msg.Author);

            await embedrep.Send(msg, builder.Build());
        }

        private async Task Invite(CommandReplyEmbed embedrep,SocketMessage msg,List<string> args)
        {
            string invite = "https://discordapp.com/oauth2/authorize?client_id=" + EBotCredentials.BOT_ID_MAIN + "&scope=bot&permissions=0";
            await embedrep.Good(msg, "Invite", invite);
        }

        private async Task Lua(CommandReplyEmbed embedrep,SocketMessage msg,List<string> args)
        {
            string code = string.Join(',', args);
            List<Object> returns = new List<object>();
            bool success = LuaEnv.Run(msg,code,out returns,out string error,this.Log);

            if (success)
            {
                string display = string.Join('\t', returns);
                if (string.IsNullOrWhiteSpace(display))
                {
                    await embedrep.Good(msg, "Lua", ":ok_hand: (nil or no value was returned)");
                }
                else
                {
                    await embedrep.Good(msg, "Lua",display);
                }
            }
            else
            {
                await embedrep.Danger(msg,"Lua",error);
            }
        }

        private async Task LuaReset(CommandReplyEmbed embedrep,SocketMessage msg,List<string> args)
        {
            LuaEnv.Reset((msg.Channel as SocketChannel));
            await embedrep.Good(msg, "LuaReset", "Lua state was reset for this channel");
        }

        public void Load()
        {
            this.Handler.LoadCommand("say", this.Say, "Makes the bot say something","say \"sentence\"", this.Name);
            this.Handler.LoadCommand("ping", this.Ping, "Pings the bot","ping", this.Name);
            this.Handler.LoadCommand("help", this.Help, "This command","help \"command|nothing\"", this.Name);
            this.Handler.LoadCommand("server", this.Server, "Get the information relative to the discord server","server", this.Name);
            this.Handler.LoadCommand("info", this.Info, "Get information relative to the bot","info", this.Name);
            this.Handler.LoadCommand("invite", this.Invite, "Get the invite link of the bot","invite", this.Name);
            this.Handler.LoadCommand("l", this.Lua, "Run lua code","l luacode", this.Name);
            this.Handler.LoadCommand("lr", this.LuaReset, "Reset the lua state of the channel","lr", this.Name);

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
            this.Handler.UnloadCommand("l");
            this.Handler.UnloadCommand("lr");

            this.Log.Nice("Module", ConsoleColor.Green, "Unloaded " + this.Name);
        }
    }
}
