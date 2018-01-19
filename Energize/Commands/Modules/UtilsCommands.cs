using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Logs;
using Energize.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using static Energize.Commands.CommandHandler;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis;


namespace Energize.Commands.Modules
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

        [Command(Name="l",Help="Runs lua code",Usage="l <code>")]
        private async Task Lua(CommandContext ctx)
        {
            if (LuaEnv.Run(ctx.Message,ctx.Input,out List<Object> returns,out string error,ctx.Log))
            {
                string display = string.Join('\t', returns);
                if (string.IsNullOrWhiteSpace(display))
                {
                    await ctx.EmbedReply.Good(ctx.Message, "Lua", ":ok_hand: (nil or no value was returned)");
                }
                else
                {
                    if(display.Length > 2000)
                    {
                        await ctx.EmbedReply.Danger(ctx.Message,"Lua","The output was too long to be sent");
                    }
                    else
                    {
                        await ctx.EmbedReply.Good(ctx.Message, "Lua",display);
                    }
                }
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message,"Lua","```\n" + error.Replace("`","") + "```");
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
                await ctx.EmbedReply.Danger(ctx.Message, name, "Owner only!");
                return;
            }

            bool success = false;
            if (!ctx.HasArguments)
            {
                await ctx.EmbedReply.Danger(ctx.Message, name, "You must provide a command or a module to unload!");
                return;
            }

            string arg = ctx.Arguments[0];
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
                SocketChannel chan = ctx.Client.GetChannel(EnergizeConfig.FEEDBACK_CHANNEL_ID);
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

        [Command(Name="ev",Help="lets you evaluate code in a command context",Usage="ev <csharpcode>")]
        private async Task Eval(CommandContext ctx)
        {
            RestApplication app = await ctx.RESTClient.GetApplicationInfoAsync();
            if (ctx.Message.Author.Id != app.Owner.Id)
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Eval", "Owner only!");
                return;
            }

            string code = ctx.Input;
            if(ctx.Input[ctx.Input.Length - 1] != ';')
            {
                code = code + ";";
            }

            string[] imports = {
                "System",
                "System.Collections",
                "System.Collections.Generic",
                "System.Linq",
                "System.Reflection",
                "System.Text",
                "System.Threading.Tasks",
                "Discord",
                "Discord.Net",
                "Discord.Rest",
                "Discord.WebSocket",
                "Energize.Logs",
                "Energize.Utils",
                "Energize.MemoryStream",
                "Energize.MachineLearning",
                "Energize.Commands",
                "System.Text.RegularExpressions"
            };
            ScriptOptions options = ScriptOptions.Default
                .WithImports(imports)
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)));

            try
            {
                ScriptState state = await CSharpScript.RunAsync(code, options, ctx);
                if (state != null && state.ReturnValue != null)
                {
                    string ret = state.ReturnValue.ToString();
                    if(!string.IsNullOrWhiteSpace(ret))
                    {
                        if(ret.Length > 2000)
                        {
                            ret = ret.Substring(0,1980) + "... \n**[" + (ret.Length - 2020) + "\tCHARS\tLEFT]**";
                        }
                        
                        await ctx.EmbedReply.Good(ctx.Message,"Eval",ret);
                    }
                    else
                    {
                        await ctx.EmbedReply.Warning(ctx.Message, "Eval", ":warning: (string was null or empty)");
                    }
                }
                else
                {
                    await ctx.EmbedReply.Good(ctx.Message, "Eval", ":ok_hand: (nothing or null was returned)");
                }
            }
            catch(Exception e)
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Eval", "```\n" + e.Message.Replace("`", "") + "```");
            }

        }

        /*[Command(Name="w",Help="Asks wolfram something",Usage="w <input>")]
        private async Task Wolfram(CommandContext ctx)
        {
            if(!ctx.HasArguments)
            {
                await ctx.EmbedReply.Danger(ctx.Message,"Wolfram","You didn't provide any input");
                return;
            }

            string endpoint = "";
            string result = await HTTP.Fetch(endpoint,ctx.Log);

        }*/

        public void Initialize(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand(this.Say);
            handler.LoadCommand(this.Ping);
            handler.LoadCommand(this.Lua);
            handler.LoadCommand(this.LuaReset);
            handler.LoadCommand(this.LoadCommand);
            handler.LoadCommand(this.UnloadCommand);
            handler.LoadCommand(this.Feedback);
            //handler.LoadCommand(this.Wolfram);
            handler.LoadCommand(this.Eval);
            
            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}
