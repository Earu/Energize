using EBot.Utils;
using EBot.Logs;
using System;
using System.Threading.Tasks;

namespace EBot.Commands.Modules
{
    [CommandModule(Name="Search")]
    class SearchCommands : CommandModule,ICommandModule
    {
        [Command(Name="urban",Help="Searches for a definition on urban dictionnary",Usage="urban <search>")]
        private async Task SearchUrban(CommandContext ctx)
        {
            string search = "";
            if (string.IsNullOrWhiteSpace(ctx.Arguments[0]))
            {
                ctx.EmbedReply.Danger(ctx.Message, "Urban", "No entry to look for was given");
            }
            else
            {
                int page = 0;
                if (ctx.Arguments.Count > 1)
                {
                    if (!string.IsNullOrWhiteSpace(ctx.Arguments[1]))
                    {
                        if (int.TryParse(ctx.Arguments[1].Trim(), out int temp))
                        {
                            page = temp - 1;
                        }
                    }
                }

                search = ctx.Arguments[0];
                string body = await HTTP.Fetch("http://api.urbandictionary.com/v0/define?term=" + search,ctx.Log);
                Urban.UGlobal global = JSON.Deserialize<Urban.UGlobal>(body, ctx.Log);

                if (global == null)
                {
                    ctx.EmbedReply.Danger(ctx.Message, "Urban", "There was no data to use for this!");
                }
                else
                {
                    if (global.list.Length == 0)
                    {
                        ctx.EmbedReply.Danger(ctx.Message, "Urban", "Looks like I couldn't find anything!");
                    }
                    else
                    {
                        if(global.list.Length-1 >= page && page >= 0)
                        {
                            Urban.UWord wordobj = global.list[page];
                            bool hasexample = string.IsNullOrWhiteSpace(wordobj.example);
                            string smalldef = wordobj.definition.Length > 300 ? wordobj.definition.Remove(300) + "..." : wordobj.definition;
                            ctx.EmbedReply.Good(ctx.Message, "Definition " + (page + 1) + "/" + global.list.Length,
                                "**" + wordobj.permalink + "**\n\n"
                                + smalldef + (!hasexample ? "\n\nExample:\n\n*" + wordobj.example + "*" : "") + "\n\n" +
                                ":thumbsup: x" + wordobj.thumbs_up + "\t :thumbsdown: x" + wordobj.thumbs_down);
                        }
                        else
                        {
                            ctx.EmbedReply.Danger(ctx.Message, "Urban", "No result for definition n°" + (page+1));
                        }
                    }
                }
            }
        }

        private async Task Google(CommandContext ctx)
        {
            string apikey = EBotCredentials.GOOGLE_API_KEY;
        }

        public void Initialize(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand(this.SearchUrban);

            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}
