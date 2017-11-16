using DSharpPlus.Entities;
using EBot.Utils;
using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EBot.Commands.Modules
{
    class SearchCommands : ICommandModule
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
                int page = 0;
                if (args.Count > 1)
                {
                    if (!string.IsNullOrWhiteSpace(args[1]))
                    {
                        int temp;
                        if (int.TryParse(args[1].Trim(), out temp))
                        {
                            page = temp - 1;
                        }
                    }
                }

                search = args[0];
                string body = await HTTP.Fetch("http://api.urbandictionary.com/v0/define?term=" + search,this.Log);
                Urban.UGlobal global = JSON.Deserialize<Urban.UGlobal>(body, this.Log);

                if (global == null)
                {
                    await embedrep.Danger(msg, "Err", "There was no data to use for this!");
                }
                else
                {
                    if (global.list.Length == 0)
                    {
                        await embedrep.Danger(msg, "Arf!", "Looks like I couldn't find anything!");
                    }
                    else
                    {
                        if(global.list.Length-1 >= page && page >= 0)
                        {
                            Urban.UWord wordobj = global.list[page];
                            bool hasexample = string.IsNullOrWhiteSpace(wordobj.example);
                            string smalldef = wordobj.definition.Length > 300 ? wordobj.definition.Remove(300) + "..." : wordobj.definition;
                            await embedrep.Good(msg, "Definition #" + (page + 1),
                                "**" + wordobj.permalink + "**\n\n"
                                + smalldef + (!hasexample ? "\n\nExample:\n\n*" + wordobj.example + "*" : "") + "\n\n" +
                                ":thumbsup: x" + wordobj.thumbs_up + "\t :thumbsdown: x" + wordobj.thumbs_down);
                        }
                        else
                        {
                            await embedrep.Danger(msg, "uh", "No result for definition n°" + (page+1));
                        }
                    }
                }
            }
        }

        private async Task Google(CommandReplyEmbed embedrep,DiscordMessage msg,List<string> args)
        {
            string apikey = EBotCredentials.GOOGLE_API_KEY;
        }

        public void Load()
        {
            this.Handler.LoadCommand("urban", this.SearchUrban, "Lookup a definition on urban dictionnary","urban \"search\"",this.Name);

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload()
        {
            this.Handler.UnloadCommand("urban");

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }
    }
}
