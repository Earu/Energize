using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using EBot.Logs;
using EBot.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static EBot.Commands.CommandHandler;

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


            await ctx.EmbedReply.Good(ctx.Message, "Pong!", ":alarm_clock: Discord: " + diff + "ms\n" +
                ":clock1: Bot: " + ctx.Client.Latency + "ms");
        }

        [Command(Name="say",Help="Makes me say something",Usage="say <sentence>")]
        private async Task Say(CommandContext ctx)
        {
            if (ctx.HasArguments)
            {
                await ctx.EmbedReply.Good(ctx.Message, "Say", ctx.Input);
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Say", "You need to provide a sentence");
            }
        }

        [Command(Name="invite",Help="Gets the invite link for the bot",Usage="invite <nothing>")]
        private async Task Invite(CommandContext ctx)
        {
            string invite = "https://discordapp.com/oauth2/authorize?client_id=" + EBotConfig.BOT_ID_MAIN + "&scope=bot&permissions=8";
            string server = "https://discord.gg/KJqhQ22";

            await ctx.EmbedReply.Good(ctx.Message, "Invite", "[Invite EBot](" + invite + ")\t\t\t[Join EBot's server](" + server + ")");
        }

        [Command(Name="l",Help="Runs lua code",Usage="l <code>")]
        private async Task Lua(CommandContext ctx)
        {
            string code = ctx.Input;
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

        [Command(Name="lr",Help="Reset the lua state of the channel",Usage="lr <nothing>")]
        private async Task LuaReset(CommandContext ctx)
        {
            LuaEnv.Reset(ctx.Message.Channel.Id,ctx.Log);
            await ctx.EmbedReply.Good(ctx.Message, "Lua Reset", "Lua state was reset for this channel");
        }

        private async Task BehaviorChange(CommandContext ctx,string name, Action<CommandCallback> callback)
        {
            RestApplication app = await ctx.RESTClient.GetApplicationInfoAsync();
            if (ctx.Message.Author.Id != app.Owner.Id)
            {
                await ctx.EmbedReply.Danger(ctx.Message, name, "You are not the owner of this bot!");
                return;
            }

            bool success = false;
            if (!ctx.HasArguments)
            {
                await ctx.EmbedReply.Danger(ctx.Message, name, "You must provide a command or a module to unload!");
                return;
            }

            string arg = ctx.Arguments[0].Trim();
            if (Command.Modules.ContainsKey(arg))
            {
                Command.Modules[arg].ForEach(x => callback(x.Callback));
                Command.SetLoadedModule(arg, false);

                await ctx.EmbedReply.Good(ctx.Message, name, "Successfully " + name.ToLower() + "ed module \"" + arg + "\"");
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
                            callback(cmd.Callback);

                            await ctx.EmbedReply.Good(ctx.Message, name, name + "ed command \"" + arg + "\"");
                            success = true;
                            break;
                        }
                    }
                }
            }

            if (!success)
            {
                await ctx.EmbedReply.Danger(ctx.Message, name, "No command or module with that name was found!");
            }
        }

        [Command(Name="unload",Help="Unloads a module or a command, owner only",Usage="unload <cmd|module>")]
        private async Task UnloadCommand(CommandContext ctx)
        {
            await this.BehaviorChange(ctx, "Unload", ctx.Handler.UnloadCommand);
        }

        [Command(Name="load",Help="Loads a module or a command, owner only",Usage="load <cmd|module>")]
        private async Task LoadCommand(CommandContext ctx)
        {
            await this.BehaviorChange(ctx, "Load", ctx.Handler.LoadCommand);
        }

        [Command(Name="feedback",Help="Give feedback to the owner, suggestion or bugs",Usage="feedback <sentence>")]
        private async Task Feedback(CommandContext ctx)
        {
            if (ctx.HasArguments)
            {
                string feedback = ctx.Input;
                SocketChannel chan = ctx.Client.GetChannel(EBotConfig.FEEDBACK_CHANNEL_ID);
                await ctx.EmbedReply.Good(ctx.Message, "Feedback", "Successfully sent your feedback");

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(ctx.EmbedReply.ColorNormal);
                builder.WithAuthor(ctx.Message.Author);
                builder.WithTimestamp(ctx.Message.CreatedAt);
                builder.WithDescription(ctx.Input);
                if(ctx.Message.Channel is IGuildChannel)
                {
                    IGuildChannel c = ctx.Message.Channel as IGuildChannel;
                    builder.WithFooter(c.Guild.Name + "#" + c.Name + " Feedback");
                }
                else
                {
                    builder.WithFooter("DM Feedback");
                }

                await ctx.EmbedReply.Send(chan,builder.Build());
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Feedback", "You can't send nothing!");
            }
        }

        public void Initialize(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand(this.Say);
            handler.LoadCommand(this.Ping);
            handler.LoadCommand(this.Invite);
            handler.LoadCommand(this.Lua);
            handler.LoadCommand(this.LuaReset);
            handler.LoadCommand(this.LoadCommand);
            handler.LoadCommand(this.UnloadCommand);
            handler.LoadCommand(this.Feedback);
            
            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}
