using EBot.Utils;
using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;

namespace EBot.Commands.Modules
{
    class NSFWCommands : ICommandModule
    {
        private string Name = "NSFW";

        private async Task SearchE621(CommandContext ctx)
        {
            if (ctx.Message.Channel.IsNsfw)
            {
                Random rand = new Random();
                string body = await HTTP.Fetch("https://e621.net/post/index.json",ctx.Log);
                List<E621.EPost> posts = JSON.Deserialize<List<E621.EPost>>(body,ctx.Log);
                E621.EPost post;

                if(posts == null)
                {
                    await ctx.EmbedReply.Danger(ctx.Message, "Err", "There was no data to use sorry!");
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
                        await ctx.EmbedReply.Danger(ctx.Message, "Nooo", "Seems like I couldn't find anything!");
                    }
                    else
                    {
                        EmbedBuilder embed = new EmbedBuilder
                        {
                            Color = new Color(110, 220, 110),
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
                await ctx.EmbedReply.Danger(ctx.Message, "Hum no.", "Haha, did you really believe it would be that easy? :smirk:");
            }
        }

        public void Load(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand("e621", this.SearchE621, "Browse e621","e621 \"search\"",this.Name);

            log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload(CommandHandler handler,BotLog log)
        {
            handler.UnloadCommand("e621");

            log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }
    }
}
