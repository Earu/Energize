using EBot.Utils;
using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;

namespace EBot.Commands.Modules
{
    [CommandModule(Name="NSFW")]
    class NSFWCommands : CommandModule,ICommandModule
    {
        [Command(Name="e621",Help="Browses E621",Usage="e621 <search>")]
        private async Task SearchE621(CommandContext ctx)
        {
            if (ctx.IsNSFW())
            {
                Random rand = new Random();
                string body = await HTTP.Fetch("https://e621.net/post/index.json",ctx.Log);
                List<E621.EPost> posts = JSON.Deserialize<List<E621.EPost>>(body,ctx.Log);
                E621.EPost post;

                if(posts == null)
                {
                    await ctx.EmbedReply.Danger(ctx.Message, "E621", "There was no data to use sorry!");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(ctx.Arguments[0]))
                    {
                        post = E621.SortingHandler.GetRandom(posts);
                    }
                    else
                    {
                        post = E621.Search.Handle(posts, ctx.Arguments);
                    }

                    if (post == null)
                    {
                         await ctx.EmbedReply.Danger(ctx.Message, "E621", "Seems like I couldn't find anything!");
                    }
                    else
                    {
                        EmbedBuilder embed = new EmbedBuilder
                        {
                            Color = ctx.EmbedReply.ColorGood,
                            ImageUrl = post.sample_url,
                            Title = "E621",
                            Description = post.sample_url + "\n*Width: " + post.sample_width + "\tHeight: " + post.sample_height + "*"
                        };

                        await ctx.EmbedReply.Send(ctx.Message, embed.Build());
                    }
                }
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "E621", "Haha, did you really believe it would be that easy? :smirk:");
            }
        }

        public void Initialize(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand(this.SearchE621);

            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}
