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

        private async Task Ping(CommandContext ctx)
        {
            DateTimeOffset createtimestamp = ctx.Message.CreatedAt;
            DateTimeOffset timestamp = ctx.Message.Timestamp;

            int diff = timestamp.Millisecond / 10;


            await ctx.EmbedReply.Good(ctx.Message, "Pong!", ":alarm_clock: Discord: " + diff + "ms\n" +
                ":clock1: Bot: " + ctx.Client.Latency + "ms");
        }

        private async Task Help(CommandContext ctx)
        {
            string arg = ctx.Arguments[0];
            if (!string.IsNullOrWhiteSpace(arg))
            {
                bool retrieved = ctx.Commands.TryGetValue(arg.ToLower().Trim(), out Command cmd);
                if (retrieved)
                {
                    await ctx.EmbedReply.Good(ctx.Message, "Help [ " + arg + " ]", cmd.GetHelp());
                }
                else
                {
                    await ctx.EmbedReply.Danger(ctx.Message, "Help", "Couldn't find documentation for \"" + arg + "\"");
                }
            }
            else
            {
                if (!(ctx.Message.Channel is IDMChannel))
                {
                    await ctx.EmbedReply.Good(ctx.Message, "Help", "Check your private messages " + ctx.Message.Author.Mention);
                }

                string result = "";
                foreach (KeyValuePair<string,List<Command>> module in Command.Modules)
                {
                    List<Command> cmds = module.Value;
                    result += "**" + module.Key.ToUpper() + ":**\n";
                    result += "``";
                    foreach(Command cmd in cmds)
                    {
                        result += cmd.Cmd + ",";
                    }
                    result = result.Remove(result.Length - 1);
                    result += "``\n\n";
                }

                await ctx.EmbedReply.RespondByDM(ctx.Message, "Help [ all ]", result, new Color());
            }
        }

        private async Task Say(CommandContext ctx)
        {
            string tosay = string.Join(",", ctx.Arguments);
            await ctx.EmbedReply.Good(ctx.Message,"Say", tosay);
        }

        private async Task Server(CommandContext ctx)
        {
            if (ctx.Message.Channel is IGuildChannel)
            {
                SocketGuild guild = (ctx.Message.Channel as IGuildChannel).Guild as SocketGuild;
                RestUser owner = await ctx.RESTClient.GetUserAsync(guild.OwnerId);

                string info = "";
                info += "**ID**: " + guild.Id + "\n";
                info += "**OWNER**: " + (owner == null ? "NULL\n" : owner.Username + "#" + owner.Discriminator + "\n");
                info += "**MEMBERS**: " + guild.MemberCount + "\n";
                info += "**REGION**: " + guild.VoiceRegionId + "\n";

                if (guild.Emotes.Count > 0)
                {
                    info += "\n\n**---- EMOJIS ----**\n";

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
                builder.WithAuthor(ctx.Message.Author);
            
                await ctx.EmbedReply.Send(ctx.Message, builder.Build());
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Hey!", "You can't do that in a DM channel!");
            }
        }

        private async Task Info(CommandContext ctx)
        {
            ClientInfo info = await ClientMemoryStream.GetClientInfo();

            string desc = "";
            desc += "**NAME**: " + info.Name + "\n";
            desc += "**PREFIX**: " + info.Prefix + "\n";
            desc += "**COMMANDS**: " + info.CommandAmount + "\n";
            desc += "**SERVERS**: " + info.GuildAmount + "\n";
            desc += "**USERS**: " + info.UserAmount + "\n";
            desc += "**OWNER**: " + info.Owner + "\n";

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithFooter("Info");
            builder.WithThumbnailUrl(info.Avatar);
            builder.WithDescription(desc);
            builder.WithColor(new Color());
            builder.WithAuthor(ctx.Message.Author);

            await ctx.EmbedReply.Send(ctx.Message, builder.Build());
        }

        private async Task Invite(CommandContext ctx)
        {
            string invite = "https://discordapp.com/oauth2/authorize?client_id=" + EBotCredentials.BOT_ID_MAIN + "&scope=bot&permissions=0";
            await ctx.EmbedReply.Good(ctx.Message, "Invite", invite);
        }

        private async Task Lua(CommandContext ctx)
        {
            string code = string.Join(',', ctx.Arguments);
            List<Object> returns = new List<object>();
            bool success = LuaEnv.Run(ctx.Message,code,out returns,out string error,ctx.Log);
       
            if (success)
            {
                string display = string.Join('\t', returns);
                if (string.IsNullOrWhiteSpace(display))
                {
                    await ctx.EmbedReply.Good(ctx.Message, "Lua", ":ok_hand: (nil or no value was returned)");
                }
                else
                {
                    await ctx.EmbedReply.Good(ctx.Message, "Lua",display);
                }
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message,"Lua",error);
            }
        }

        private async Task LuaReset(CommandContext ctx)
        {
            LuaEnv.Reset(ctx.Message.Channel.Id);
            await ctx.EmbedReply.Good(ctx.Message, "LuaReset", "Lua state was reset for this channel");
        }

        public void Load(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand("say", this.Say, "Makes the bot say something","say \"sentence\"", this.Name);
            handler.LoadCommand("ping", this.Ping, "Pings the bot","ping", this.Name);
            handler.LoadCommand("help", this.Help, "This command","help \"command|nothing\"", this.Name);
            handler.LoadCommand("server", this.Server, "Get the information relative to the discord server","server", this.Name);
            handler.LoadCommand("info", this.Info, "Get information relative to the bot","info", this.Name);
            handler.LoadCommand("invite", this.Invite, "Get the invite link of the bot","invite", this.Name);
            handler.LoadCommand("l", this.Lua, "Run lua code","l luacode", this.Name);
            handler.LoadCommand("lr", this.LuaReset, "Reset the lua state of the channel","lr", this.Name);

            log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload(CommandHandler handler,BotLog log)
        {
            handler.UnloadCommand("say");
            handler.UnloadCommand("ping");
            handler.UnloadCommand("help");
            handler.UnloadCommand("server");
            handler.UnloadCommand("info");
            handler.UnloadCommand("invite");
            handler.UnloadCommand("l");
            handler.UnloadCommand("lr");

            log.Nice("Module", ConsoleColor.Green, "Unloaded " + this.Name);
        }
    }
}
