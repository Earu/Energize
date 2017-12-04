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
            if (!ctx.IsNSFW())
            {
                await ctx.EmbedReply.Danger(ctx.Message, "E621", "Haha, did you really believe it would be that easy? :smirk:");
                return;
            }

            Random rand = new Random();
            string body = await HTTP.Fetch("https://e621.net/post/index.json?tags=" + ctx.Input,ctx.Log);
            List<E621.EPost> posts = JSON.Deserialize<List<E621.EPost>>(body,ctx.Log);

            if(posts == null)
            {
                await ctx.EmbedReply.Danger(ctx.Message, "E621", "There was no data to use sorry!");
            }
            else
            {
                E621.EPost post = posts[rand.Next(0, posts.Count - 1)];
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(ctx.EmbedReply.ColorGood);
                builder.WithImageUrl(post.sample_url);
                builder.WithAuthor(ctx.Message.Author);
                builder.WithFooter("E621");

                await ctx.EmbedReply.Send(ctx.Message, builder.Build());
            }
        }

        public void Initialize(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand(this.SearchE621);

            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}
