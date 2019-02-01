using Energize.Utils;
using System.Threading.Tasks;
using YoutubeSearch;
using System.Collections.Generic;

namespace Energize.Services.Commands.Modules
{
    [CommandModule("Search")]
    class SearchCommands
    {
        [Command(Name="urban",Help="Searches for a definition on urban dictionnary",Usage="urban <search>,<pagenumber|nothing>")]
        private async Task SearchUrban(CommandContext ctx)
        {
            if(!ctx.IsNSFW())
            {
                await ctx.MessageSender.Warning(ctx.Message, "Urban", "Apparently Urban Dictionary is NSFW nowadays 🤷");
                return;
            }

            if (!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
                return;
            }

            int page = 0;
            if (ctx.Arguments.Count > 1)
                if (!string.IsNullOrWhiteSpace(ctx.Arguments[1]))
                    if (int.TryParse(ctx.Arguments[1].Trim(), out int temp))
                        page = temp - 1;

            string search = ctx.Arguments[0];
            string body = await HTTP.Fetch("http://api.urbandictionary.com/v0/define?term=" + search,ctx.Log);
            Urban.UGlobal global = JSON.Deserialize<Urban.UGlobal>(body, ctx.Log);

            if (global == null)
                await ctx.MessageSender.Danger(ctx.Message, "Urban", "There was no data to use for this!");
            else
            {
                if (global.list.Length == 0)
                    await ctx.MessageSender.Danger(ctx.Message, "Urban", "Looks like I couldn't find anything!");
                else
                {
                    if(global.list.Length-1 < page)
                        page = 0;

                    if(page < 0)
                        page = global.list.Length - 1;

                    Urban.UWord wordobj = global.list[page];
                    bool hasexample = string.IsNullOrWhiteSpace(wordobj.example);
                    string smalldef = wordobj.definition.Length > 300 ? wordobj.definition.Remove(300) + "..." : wordobj.definition;
                    await ctx.MessageSender.Good(ctx.Message, "Definition " + (page + 1) + "/" + global.list.Length,
                        "**" + wordobj.permalink + "**\n\n"
                        + smalldef + (!hasexample ? "\n\n**EXAMPLE:**\n\n" + wordobj.example : "") + "\n\n" +
                        ":thumbsup: x" + wordobj.thumbs_up + "\t :thumbsdown: x" + wordobj.thumbs_down);
                }
            }
        }

        [Command(Name="yt",Help="Searches youtube",Usage="yt <search>,<pagenumber|nothing>")]
        private async Task Youtube(CommandContext ctx)
        {
            if (!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
                return;
            }

            VideoSearch items = new VideoSearch();
            List<VideoInformation> videos = null;
            try
            {
                videos = items.SearchQuery(ctx.Arguments[0], 1);
            }
            catch
            {
                await ctx.MessageSender.Danger(ctx.Message, "YT", "Couldn't find anything for given search");
                return;
            }

            if (videos.Count > 0)
            {
                if (ctx.Arguments.Count == 2)
                {
                    string pageid = ctx.Arguments[1].Trim();
                    if(int.TryParse(pageid,out int id))
                    {
                        if (id > videos.Count)
                            id = 1;
                                
                        if(id < 1)
                            id = videos.Count;

                        VideoInformation video = videos[id-1];
                        await ctx.MessageSender.SendRaw(ctx.Message, ctx.AuthorMention + " #" + id 
                        + " out of " + videos.Count + " results for \"" + ctx.Arguments[0] + "\"" 
                        + "\n" + video.Url);
                    }
                    else
                    {
                        await ctx.SendBadUsage();
                    }
                }
                else
                {
                    VideoInformation video = videos[0];
                    await ctx.MessageSender.SendRaw(ctx.Message,ctx.AuthorMention + " #1 out of " + videos.Count
                            + " results for \"" + ctx.Input + "\"" + "\n" + video.Url);
                }
            }
            else
            {
                await ctx.MessageSender.Danger(ctx.Message, "YT", "Couldn't find anything for given search");
            }
        }

        /*[Command(Name="g",Help="Gets a result from google",Usage="g <search>,<resultnumber>")]
        private async Task Google(CommandContext ctx)
        {
            string endpoint = $"https://www.googleapis.com/customsearch/v1?key={EnergizeConfig.GOOGLE_API_KEY}"
                + $"&cx=010399579583698952911:lc0stqwbymq&q={ctx.Arguments[0]}";
            string json = await HTTP.Fetch(endpoint, ctx.Log);
        }*/
    }
}
