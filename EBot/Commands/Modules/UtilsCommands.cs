using DSharpPlus.Entities;
using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EBot.Commands.Modules
{
    class UtilsCommands
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
            try
            {
                await msg.DeleteAsync();
            }
            catch
            {
                this.Log.Nice("Commands", ConsoleColor.Red, "<say> couldn't remove the user message!");
            }
        }

        public void Load()
        {
            this.Handler.LoadCommand("say", this.Say, "Makes the bot say something!",this.Name);
            this.Handler.LoadCommand("ping", this.Ping, "Pings the bot",this.Name);
            this.Handler.LoadCommand("help", this.Help, "Shows help for each command",this.Name);

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
