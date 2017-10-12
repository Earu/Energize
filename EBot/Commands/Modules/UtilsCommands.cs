using DSharpPlus.Entities;
using EBot.Logs;
using System;
using System.Collections.Generic;

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

        private void Ping(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            DateTimeOffset createtimestamp = msg.CreationTimestamp;
            DateTimeOffset timestamp = msg.Timestamp;

            int diff = (createtimestamp.Millisecond - timestamp.Millisecond) / 10;


            embedrep.Good(msg, "Pong!", ":alarm_clock: Discord: " + diff + "ms\n" +
                ":clock1: Bot: " + this.Handler.Client.Ping + "ms");
        }

        private void Help(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            string arg = args[0];
            if (!string.IsNullOrWhiteSpace(arg))
            {
                string desc = "";
                bool retrieved = this.Handler.CommandsHelp.TryGetValue(arg.ToLower().Trim(), out desc);
                if (retrieved)
                {
                    embedrep.Warning(msg, "Help", "***" + arg + "*** : " + desc);
                }
                else
                {
                    embedrep.Danger(msg, "Help", "Couldn't find documentation for \"" + arg + "\"");
                }
            }
            else
            {
                string final = "";
                foreach (KeyValuePair<string, string> cmd in this.Handler.CommandsHelp)
                {
                    final += "***" + cmd.Key + "*** :" + "\n" + cmd.Value + "\n";
                }
                embedrep.Warning(msg, "Help", final);
            }
        }

        private async void Say(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            string tosay = string.Join(",", args.ToArray());
            embedrep.Good(msg, msg.Author.Username, tosay);
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
            this.Handler.LoadCommand("say", this.Say, "Makes the bot say something!");
            this.Handler.LoadCommand("ping", this.Ping, "Pings the bot");
            this.Handler.LoadCommand("help", this.Help, "Shows help for each command");

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
