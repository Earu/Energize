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
    [CommandModule(Name="Utils")]
    class UtilsCommands : CommandModule,ICommandModule
    {
        [Command(Name="ping",Help="Pings the bot",Usage="ping <nothing>")]
        private async Task Ping(CommandContext ctx)
        {
            DateTimeOffset createtimestamp = ctx.Message.CreatedAt;
            DateTimeOffset timestamp = ctx.Message.Timestamp;

            int diff = timestamp.Millisecond / 10;


            ctx.EmbedReply.Good(ctx.Message, "Pong!", ":alarm_clock: Discord: " + diff + "ms\n" +
                ":clock1: Bot: " + ctx.Client.Latency + "ms");
        }

        [Command(Name="help",Help="This command",Usage="help <command|nothing>")]
        private async Task Help(CommandContext ctx)
        {
            string arg = ctx.Arguments[0];
            if (!string.IsNullOrWhiteSpace(arg))
            {
                bool retrieved = ctx.Commands.TryGetValue(arg.ToLower().Trim(), out Command cmd);
                if (retrieved && cmd.Loaded)
                {
                    ctx.EmbedReply.Good(ctx.Message, "Help [ " + arg + " ]", cmd.GetHelp());
                }
                else
                {
                    ctx.EmbedReply.Danger(ctx.Message, "Help", "Couldn't find documentation for \"" + arg + "\"");
                }
            }
            else
            {
                if (!ctx.IsPrivate)
                {
                    ctx.EmbedReply.Good(ctx.Message, "Help", "Check your private messages " + ctx.Message.Author.Mention);
                }

                string result = "";
                foreach (KeyValuePair<string,List<Command>> module in Command.Modules)
                {
                    if (Command.IsLoadedModule(module.Key))
                    {
                        List<Command> cmds = module.Value;
                        result += "**" + module.Key.ToUpper() + ":**\n";
                        result += "``";
                        foreach (Command cmd in cmds)
                        {
                            if (cmd.Loaded)
                            {
                                result += cmd.Cmd + ",";
                            }
                        }
                        result = result.Remove(result.Length - 1);
                        result += "``\n\n";
                    }
                }

                await ctx.EmbedReply.RespondByDM(ctx.Message, "Help [ all ]", result);
            }
        }

        [Command(Name="say",Help="Makes me say something",Usage="say <sentence>")]
        private async Task Say(CommandContext ctx)
        {
            string tosay = string.Join(",", ctx.Arguments);
            ctx.EmbedReply.Good(ctx.Message,"Say", tosay);
        }

        [Command(Name="server",Help="Gets information about the server",Usage="server <nothing>")]
        private async Task Server(CommandContext ctx)
        {
            if (!ctx.IsPrivate)
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
                builder.WithColor(ctx.EmbedReply.ColorGood);
                builder.WithAuthor(ctx.Message.Author);
            
                ctx.EmbedReply.Send(ctx.Message, builder.Build());
            }
            else
            {
                ctx.EmbedReply.Danger(ctx.Message, "Server", "You can't do that in a DM channel!");
            }
        }

        [Command(Name="info",Help="Gets information relative to the bot",Usage="info <nothing>")]
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
            builder.WithColor(ctx.EmbedReply.ColorGood);
            builder.WithAuthor(ctx.Message.Author);

            ctx.EmbedReply.Send(ctx.Message, builder.Build());
        }

        [Command(Name="invite",Help="Gets the invite link for the bot",Usage="invite <nothing>")]
        private async Task Invite(CommandContext ctx)
        {
            string invite = "https://discordapp.com/oauth2/authorize?client_id=" + EBotCredentials.BOT_ID_MAIN + "&scope=bot&permissions=0";
            ctx.EmbedReply.Good(ctx.Message, "Invite", invite);
        }

        [Command(Name="l",Help="Runs lua code",Usage="l <code>")]
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
                    ctx.EmbedReply.Good(ctx.Message, "Lua", ":ok_hand: (nil or no value was returned)");
                }
                else
                {
                    ctx.EmbedReply.Good(ctx.Message, "Lua",display);
                }
            }
            else
            {
                ctx.EmbedReply.Danger(ctx.Message,"Lua",error);
            }
        }

        [Command(Name="lr",Help="Reset the lua state of the channel",Usage="lr <nothing>")]
        private async Task LuaReset(CommandContext ctx)
        {
            LuaEnv.Reset(ctx.Message.Channel.Id,ctx.Log);
            ctx.EmbedReply.Good(ctx.Message, "Lua Reset", "Lua state was reset for this channel");
        }

        [Command(Name="unload",Help="Unloads a module or a command, owner only",Usage="unload <cmd|module>")]
        private async Task UnloadCommand(CommandContext ctx)
        {
            RestApplication app = await ctx.RESTClient.GetApplicationInfoAsync();
            if(ctx.Message.Author.Id == app.Owner.Id)
            {
                bool success = false;
                if (!string.IsNullOrWhiteSpace(ctx.Arguments[0]))
                {
                    string arg = ctx.Arguments[0].Trim();
                    if (Command.Modules.ContainsKey(arg))
                    {
                        foreach(Command cmd in Command.Modules[arg])
                        {
                            ctx.Handler.UnloadCommand(cmd.Callback);
                        }

                        Command.SetLoadedModule(arg, false);

                        ctx.EmbedReply.Good(ctx.Message, "Unload", "Successfully unloaded module \"" + arg + "\"");
                        success = true;
                    }
                    else
                    {
                        foreach(KeyValuePair<string,List<Command>> module in Command.Modules)
                        {
                            foreach(Command cmd in module.Value)
                            {
                                if (cmd.Cmd == arg)
                                {
                                    ctx.Handler.UnloadCommand(cmd.Callback);

                                    ctx.EmbedReply.Good(ctx.Message, "Unload", "Unloaded command \"" + arg + "\"");
                                    success = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!success)
                    {
                        ctx.EmbedReply.Danger(ctx.Message, "Unload", "No command or module with that name was found!");
                    }
                }
                else
                {
                    ctx.EmbedReply.Danger(ctx.Message, "Unload", "You must provide a command or a module to unload!");
                }
            }
            else
            {
                ctx.EmbedReply.Danger(ctx.Message, "Unload", "You are not the owner of this bot!");
            }
        }

        [Command(Name="load",Help="Loads a module or a command, owner only",Usage="load <cmd|module>")]
        private async Task LoadCommand(CommandContext ctx)
        {
            RestApplication app = await ctx.RESTClient.GetApplicationInfoAsync();
            if (ctx.Message.Author.Id == app.Owner.Id)
            {
                bool success = false;
                if (!string.IsNullOrWhiteSpace(ctx.Arguments[0]))
                {
                    string arg = ctx.Arguments[0].Trim();
                    if (Command.Modules.ContainsKey(arg))
                    {
                        foreach (Command cmd in Command.Modules[arg])
                        {
                            ctx.Handler.LoadCommand(cmd.Callback);
                        }

                        Command.SetLoadedModule(arg, true);

                        ctx.EmbedReply.Good(ctx.Message, "Load", "Successfully loaded module \"" + arg + "\"");
                        success = true;
                    }
                    else
                    {
                        foreach (KeyValuePair<string, List<Command>> module in Command.Modules)
                        {
                            foreach (Command cmd in module.Value)
                            {
                                if (cmd.Cmd == arg)
                                {
                                    ctx.Handler.LoadCommand(cmd.Callback);

                                    ctx.EmbedReply.Good(ctx.Message, "Load", "Loaded command \"" + arg + "\"");
                                    success = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!success)
                    {
                        ctx.EmbedReply.Danger(ctx.Message, "Load", "No command or module with that name was found!");
                    }
                }
                else
                {
                    ctx.EmbedReply.Danger(ctx.Message, "Load", "You must provide a command or a module to unload!");
                }
            }
            else
            {
                ctx.EmbedReply.Danger(ctx.Message, "Load", "You are not the owner of this bot!");
            }
        }

        public void Initialize(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand(this.Say);
            handler.LoadCommand(this.Ping);
            handler.LoadCommand(this.Help);
            handler.LoadCommand(this.Server);
            handler.LoadCommand(this.Info);
            handler.LoadCommand(this.Invite);
            handler.LoadCommand(this.Lua);
            handler.LoadCommand(this.LuaReset);
            handler.LoadCommand(this.LoadCommand);
            handler.LoadCommand(this.UnloadCommand);

            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}
