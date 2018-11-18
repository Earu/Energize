using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using static Energize.Services.Commands.CommandHandler;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Energize.Services.LuaService;
using System.IO;
using System.Diagnostics;
using Energize.Services.Listeners;
using Microsoft.Data.Sqlite;

namespace Energize.Services.Commands.Modules
{
    [CommandModule("Utils")]
    class UtilsCommands
    {
        [Command(Name="ping",Help="Pings the bot",Usage="ping <nothing>")]
        private async Task Ping(CommandContext ctx)
        {
            DateTimeOffset timestamp = ctx.Message.Timestamp;
            int diff = timestamp.Millisecond / 10;

            await ctx.MessageSender.Good(ctx.Message, "Pong!", $":alarm_clock: Discord: {diff}ms\n" +
                $":clock1: Bot: {ctx.Client.Latency}ms");
        }

        [Command(Name="usage",Help="Gets CPU usage and memory usage",Usage="usage <nothing>")]
        private async Task Usage(CommandContext ctx)
        {
            int cores = 8;
            Process curprocess = Process.GetCurrentProcess();
            int cpu = (int)(curprocess.TotalProcessorTime.TotalMilliseconds / 1000 / cores);
            int mbused = (int)(curprocess.WorkingSet64 / 1024 / 1024);

            await ctx.MessageSender.Good(ctx.Message,"Usage", $":gear: CPU: {cpu}%\n" +
                $":wrench: Memory: {mbused}MB");
        }

        [Command(Name="uptime",Help="Gets the current uptime",Usage="uptime <nothing>")]
        private async Task Uptime(CommandContext ctx)
        {
            TimeSpan diff = (DateTime.Now - Process.GetCurrentProcess().StartTime).Duration();
            string res = $"{diff.Days}d{diff.Hours}h{diff.Minutes}m";

            await ctx.MessageSender.Good(ctx.Message, "Uptime", "The current instance has been up for " + res);
        }

        [Command(Name="say",Help="Makes me say something",Usage="say <sentence>")]
        private async Task Say(CommandContext ctx)
        {
            if (ctx.HasArguments)
            {
                await ctx.MessageSender.Good(ctx.Message, "Say", ctx.Input);
            }
            else
            {
                await ctx.SendBadUsage();
            }
        }

        [Command(Name="l",Help="Runs lua code",Usage="l <code>")]
        private async Task Lua(CommandContext ctx)
        {
            LuaEnv env = ServiceManager.GetService<LuaEnv>("Lua");
            if (env.Run(ctx.Message,ctx.Input,out List<Object> returns,out string error,ctx.Log))
            {
                string display = string.Join('\t', returns);
                if (string.IsNullOrWhiteSpace(display))
                {
                    await ctx.MessageSender.Good(ctx.Message, "Lua", ":ok_hand: (nil or no value was returned)");
                }
                else
                {
                    if(display.Length > 2000)
                    {
                        await ctx.MessageSender.Danger(ctx.Message,"Lua","The output was too long to be sent");
                    }
                    else
                    {
                        await ctx.MessageSender.Good(ctx.Message, "Lua",display);
                    }
                }
            }
            else
            {
                await ctx.MessageSender.Danger(ctx.Message,"Lua","```\n" + error.Replace("`","") + "```");
            }
        }

        [Command(Name="lr",Help="Reset the lua state of the channel",Usage="lr <nothing>")]
        private async Task LuaReset(CommandContext ctx)
        {
            LuaEnv env = ServiceManager.GetService<LuaEnv>("Lua");
            env.Reset(ctx.Message.Channel.Id,ctx.Log);
            await ctx.MessageSender.Good(ctx.Message, "Lua Reset", "Lua state was reset for this channel");
        }

        private async Task BehaviorChange(CommandContext ctx,string name, Action<CommandCallback> callback)
        {
            if (!await ctx.IsOwner())
            {
                await ctx.MessageSender.Danger(ctx.Message, name, "Owner only!");
                return;
            }

            bool success = false;
            if (!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
                return;
            }

            string arg = ctx.Arguments[0];
            if (Command.Modules.ContainsKey(arg))
            {
                Command.Modules[arg].ForEach(x => callback(x.Callback));
                Command.SetLoadedModule(arg, false);

                await ctx.MessageSender.Good(ctx.Message, name, "Successfully " + name.ToLower() + "d module \"" + arg + "\"");
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

                            await ctx.MessageSender.Good(ctx.Message, name, name + "d command \"" + arg + "\"");
                            success = true;
                            break;
                        }
                    }
                }
            }

            if (!success)
            {
                await ctx.MessageSender.Danger(ctx.Message, name, "No command or module with that name was found!");
            }
        }

        [Command(Name="disable",Help="Disables a module or a command, owner only",Usage="disable <cmd|module>")]
        private async Task UnloadCommand(CommandContext ctx)
        {
            await this.BehaviorChange(ctx, "Disable", ctx.Handler.UnloadCommand);
        }

        [Command(Name="enable",Help="Enables a module or a command, owner only",Usage="enable <cmd|module>")]
        private async Task LoadCommand(CommandContext ctx)
        {
            await this.BehaviorChange(ctx, "Enable", ctx.Handler.LoadCommand);
        }

        [Command(Name="feedback",Help="Give feedback to the owner, suggestion or bugs",Usage="feedback <sentence>")]
        private async Task Feedback(CommandContext ctx)
        {
            if (ctx.HasArguments)
            {
                WebhookSender sender = ServiceManager.GetService<WebhookSender>("Webhook");
                string feedback = ctx.Input;
                string name = ctx.Message.Author.Username;
                string avatar = ctx.Message.Author.GetAvatarUrl(ImageFormat.Auto);
                SocketChannel chan = ctx.Client.GetChannel(EnergizeConfig.FEEDBACK_CHANNEL_ID);
                string log = string.Empty;
                if (ctx.IsPrivate)
                {
                    IGuildChannel c = ctx.Message.Channel as IGuildChannel;
                    log += $"{c.Guild.Name}#{c.Name}";
                }
                else
                {
                    log += ctx.Message.Author.ToString();
                }

                await ctx.MessageSender.Good(ctx.Message, "Feedback", "Successfully sent your feedback");

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithDescription(feedback);
                builder.WithTimestamp(ctx.Message.CreatedAt);
                builder.WithFooter(log);

                await sender.SendEmbed(chan as ITextChannel, builder.Build(), name, avatar);
            }
            else
            {
                await ctx.SendBadUsage();
            }
        }

        [Command(Name="ev",Help="lets you evaluate code in a command context",Usage="ev <csharpcode>")]
        private async Task Eval(CommandContext ctx)
        {
            if (!await ctx.IsOwner())
            {
                await ctx.MessageSender.Danger(ctx.Message, "Eval", "Owner only!");
                return;
            }

            if(!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
                return;
            }

            string code = ctx.Input;
            if(ctx.Input[ctx.Input.Length - 1] != ';')
            {
                code = code + ";";
            }

            string[] imports = {
                "System",
                "System.IO",
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
                "Energize",
                "Energize.Utils",
                "Energize.Services",
                "Energize.Services.Commands",
                "System.Text.RegularExpressions",
                "System.Diagnostics"
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
                        if (ret.Length > 2000)
                        {
                            ret = $"{ret.Substring(0,1980)}... \n**[{(ret.Length - 2020)}\tCHARS\tLEFT]**";
                        }
                        
                        await ctx.MessageSender.Good(ctx.Message,"Eval",ret);
                    }
                    else
                    {
                        await ctx.MessageSender.Warning(ctx.Message, "Eval", ":warning: (string was null or empty)");
                    }
                }
                else
                {
                    await ctx.MessageSender.Good(ctx.Message, "Eval", ":ok_hand: (nothing or null was returned)");
                }
            }
            catch(Exception e)
            {
                await ctx.MessageSender.Danger(ctx.Message, "Eval", "```\n" + e.Message.Replace("`", "") + "```");
            }

        }

        [Command(Name="to",Help="Timing out test",Usage="to <seconds>")]
        private async Task TimingOut(CommandContext ctx)
        {
            if (!await ctx.IsOwner())
            {
                await ctx.MessageSender.Danger(ctx.Message, "Time Out", "Owner only!");
                return;
            }

            if(int.TryParse(ctx.Input,out int duration))
            {
                await Task.Delay(duration * 1000);
                await ctx.MessageSender.Good(ctx.Message,"Time Out","Timed out during `" + duration + "`s");
            }
            else
            {
                await ctx.SendBadUsage();
            }
        }

        [Command(Name="b64e",Help="Encodes a sentence to base64",Usage="b64e <sentence>")]
        private async Task B64Encode(CommandContext ctx)
        {
            if(!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(ctx.Input);
            string result = Convert.ToBase64String(bytes);

            if(result.Length > 2000)
            {
                await ctx.MessageSender.Danger(ctx.Message,"Base64","Output too long to be sent");
                return;
            }

            await ctx.MessageSender.Good(ctx.Message,"Base64",result);
        }

        [Command(Name="b64d",Help="Decodes a base64 sentence",Usage="b64d <sentence>")]
        private async Task B64Decode(CommandContext ctx)
        {
            if(!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
                return;
            }

            byte[] bytes = Convert.FromBase64String(ctx.Input);
            string result = Encoding.UTF8.GetString(bytes);

            if(result.Length > 2000)
            {
                await ctx.MessageSender.Danger(ctx.Message,"Base64","Output too long to be sent");
                return;
            }

            await ctx.MessageSender.Good(ctx.Message,"Base64",result);
        }

        [Command(Name="restart",Help="Restarts the bot, owner only",Usage="restart <nothing>")]
        private async Task Restart(CommandContext ctx)
        {
            if (!await ctx.IsOwner())
            {
                await ctx.MessageSender.Danger(ctx.Message, "Restart", "Owner only!");
                return;
            }

            File.WriteAllText("restartlog.txt",ctx.Message.Channel.Id.ToString());
            await ctx.MessageSender.Warning(ctx.Message,"Restart","Ok, restarting...");
            Process.GetCurrentProcess().Kill();
        }

        [Command(Name="sql",Help="Runs an sql statement in the database",Usage="sql <statement>")]
        private async Task SQL(CommandContext ctx)
        {
            if(!await ctx.IsOwner())
            {
                await ctx.MessageSender.Danger(ctx.Message, "Restart", "Owner only!");
                return;
            }

            if(!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
                return;
            }

            using (SqliteConnection connection = new SqliteConnection(EnergizeConfig.DB_CONNECTION_STRING))
            {

                try
                {
                    await connection.OpenAsync();

                    SqliteCommand cmd = new SqliteCommand(ctx.Input, connection);
                    SqliteDataReader reader = cmd.ExecuteReader();

                    if (!reader.HasRows)
                    {
                        await ctx.MessageSender.Warning(ctx.Message, "SQL", "No data was gathered for the specified statement");
                        return;
                    }

                    string res = string.Empty;
                    while (reader.Read())
                    {
                        List<string> values = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            values.Add(reader[i].ToString());
                        }

                        res += $"{string.Join('\t', values)}\n{new string('-', 50)}\n";
                    }

                    res = $"```\n{new string('-', 50)}\n{res}```";
                    if(res.Length > 2000)
                    {
                        await ctx.MessageSender.Danger(ctx.Message, "SQL", "Output was too long to be sent");
                    }
                    else
                    {
                        await ctx.MessageSender.Good(ctx.Message, "SQL", res);
                    }
          
                }
                catch(Exception e)
                {
                    await ctx.MessageSender.Danger(ctx.Message, "SQL", "```\n" + e.Message.Replace("`", "") + "```");
                }
            }

        }
    }
}
