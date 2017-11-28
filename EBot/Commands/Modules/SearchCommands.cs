using EBot.Utils;
using EBot.Logs;
using System;
using System.Threading.Tasks;
using YoutubeSearch;
using System.Collections.Generic;
using Discord;

namespace EBot.Commands.Modules
{
    [CommandModule(Name="Search")]
    class SearchCommands : CommandModule,ICommandModule
    {
        [Command(Name="urban",Help="Searches for a definition on urban dictionnary",Usage="urban <search>,<pagenumber|nothing>")]
        private async Task SearchUrban(CommandContext ctx)
        {
            if (!ctx.HasArguments)
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Urban", "No entry to look for was given");
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

                string search = ctx.Arguments[0];
                string body = await HTTP.Fetch("http://api.urbandictionary.com/v0/define?term=" + search,ctx.Log);
                Urban.UGlobal global = JSON.Deserialize<Urban.UGlobal>(body, ctx.Log);

                if (global == null)
                {
                    await ctx.EmbedReply.Danger(ctx.Message, "Urban", "There was no data to use for this!");
                }
                else
                {
                    if (global.list.Length == 0)
                    {
                        await ctx.EmbedReply.Danger(ctx.Message, "Urban", "Looks like I couldn't find anything!");
                    }
                    else
                    {
                        if(global.list.Length-1 < page)
                        {
                            page = 0;
                        }

                        if(page < 0)
                        {
                            page = global.list.Length - 1;
                        }

                        Urban.UWord wordobj = global.list[page];
                        bool hasexample = string.IsNullOrWhiteSpace(wordobj.example);
                        string smalldef = wordobj.definition.Length > 300 ? wordobj.definition.Remove(300) + "..." : wordobj.definition;
                        await ctx.EmbedReply.Good(ctx.Message, "Definition " + (page + 1) + "/" + global.list.Length,
                            "**" + wordobj.permalink + "**\n\n"
                            + smalldef + (!hasexample ? "\n\n**EXAMPLE:**\n\n" + wordobj.example : "") + "\n\n" +
                            ":thumbsup: x" + wordobj.thumbs_up + "\t :thumbsdown: x" + wordobj.thumbs_down);
                    }
                }
            }
        }

        [Command(Name="yt",Help="Searches youtube",Usage="yt <search>,<pagenumber|nothing>")]
        private async Task Youtube(CommandContext ctx)
        {
            if (ctx.HasArguments)
            {
                VideoSearch items = new VideoSearch();
                List<VideoInformation> videos = items.SearchQuery(ctx.Input, 1);
                if (videos.Count > 0)
                {
                    if (ctx.Arguments.Count == 2)
                    {
                        string pageid = ctx.Arguments[1].Trim();
                        if(int.TryParse(pageid,out int id))
                        {
                            if (id > videos.Count)
                            {
                                id = 1;
                            }
                                
                            if(id < 1)
                            {
                                id = videos.Count;
                            }

                            VideoInformation video = videos[id-1];
                            await ctx.EmbedReply.SendRaw(ctx.Message, "#" + id + " out of " + videos.Count
                                + " results for \"" + ctx.Input + "\"" + "\n" + video.Url);
                        }
                        else
                        {
                            await ctx.EmbedReply.Danger(ctx.Message, "YT", "Second argument must be a number");
                        }
                    }
                    else
                    {
                        VideoInformation video = videos[0];
                        await ctx.EmbedReply.SendRaw(ctx.Message, "#1 out of " + videos.Count
                                + " results for \"" + ctx.Input + "\"" + "\n" + video.Url);
                    }
                }
                else
                {
                    await ctx.EmbedReply.Danger(ctx.Message, "YT", "Couldn't find anything for given search");
                }
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "YT", "You didn't provide any search");
            }
        }

        public void Initialize(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand(this.SearchUrban);
            handler.LoadCommand(this.Youtube);

            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}
