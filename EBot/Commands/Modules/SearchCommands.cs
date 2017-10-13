using DSharpPlus.Entities;
using EBot.Commands.Utils;
using EBot.Logs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EBot.Commands.Modules
{
    class SearchCommands
    {
        private string Name = "Search";
        private CommandsHandler Handler;
        private BotLog Log;

        public void Setup(CommandsHandler handler, BotLog log)
        {
            this.Handler = handler;
            this. Log = log;
        }

        private async Task SearchUrban(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            string search = "";
            if (string.IsNullOrWhiteSpace(args[0]))
            {
                await embedrep.Danger(msg, "Urban", "No entry to look for was given");
            }
            else
            {
                search = args[0];
                string body = await HTTP.Fetch("http://api.urbandictionary.com/v0/define?term=" + search,this.Log);
                body = JsonConvert.DeserializeObject(body).ToString();
                Urban.UGlobal global = JsonConvert.DeserializeObject<Urban.UGlobal>(body);

                if (global.list.Length == 0)
                {
                    await embedrep.Danger(msg, "Arf!", "Looks like I couldn't find anything!");
                }
                else
                {
                    for (int i = 0; i < 3 && i < global.list.Length; i++)
                    {
                        Urban.UWord wordobj = global.list[i];
                        bool hasexample = string.IsNullOrWhiteSpace(wordobj.example);
                        string smalldef = wordobj.definition.Length > 300 ? wordobj.definition.Remove(300) + "..." : wordobj.definition;
                        await embedrep.Good(msg, "#" + (i + 1),
                            "**" + wordobj.permalink + "**\n\n"
                            + smalldef + (!hasexample ? "\n\nExample:\n\n*" + wordobj.example + "*" : "") + "\n\n" +
                            ":thumbsup: x" + wordobj.thumbs_up + "\t :thumbsdown: x" + wordobj.thumbs_down);
                    }
                }
            }
        }

        public void Load()
        {
            this.Handler.LoadCommand("urban", this.SearchUrban, "Look up a definition in urban dictionary",this.Name);

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload()
        {
            this.Handler.UnloadCommand("urban");

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }
    }
}
