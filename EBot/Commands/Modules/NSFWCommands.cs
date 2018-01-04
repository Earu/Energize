using EBot.Utils;
using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using System.Xml;

namespace EBot.Commands.Modules
{
    [CommandModule(Name="NSFW")]
    class NSFWCommands : CommandModule,ICommandModule
    {
        private Embed CreateNSFWEmbed(CommandContext ctx,string name,string pic,string url)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(ctx.EmbedReply.ColorGood);
            builder.WithImageUrl(pic);
            ctx.EmbedReply.BuilderWithAuthor(ctx.Message,builder);
            builder.WithFooter(name);
            builder.WithDescription("\n[**CHECK ON " + name.ToUpper() + "**](" + url + ")");

            return builder.Build();
        }

        [Command(Name="e621",Help="Browses E621",Usage="e621 <tags|search>")]
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
                await ctx.EmbedReply.Danger(ctx.Message, "E621", "There was a problem with your request");
            }
            else
            {
                if(posts.Count < 1)
                {
                    await ctx.EmbedReply.Danger(ctx.Message, "E621", "Nothing was found");
                    return;
                }

                E621.EPost post = posts[rand.Next(0, posts.Count - 1)];
                Embed embed = this.CreateNSFWEmbed(ctx,"E621",post.sample_url,"https://e621.net/post/show/" + post.id + "/");

                await ctx.EmbedReply.Send(ctx.Message, embed);
            }
        }

        private async Task<string[]> GetDAPIResult(CommandContext ctx,string domain)
        {
            Random rand = new Random();
            string xml = await HTTP.Fetch("http://" + domain + "/index.php?page=dapi&s=post&q=index&tags=" + ctx.Input,ctx.Log);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNodeList nodes = doc.SelectNodes("//post");
            if(nodes.Count < 1)
            {
                return new string[]{};
            }
            
            XmlNode node = nodes[rand.Next(0,nodes.Count)];
            string url = node.SelectSingleNode("@file_url").Value;
            if(url.StartsWith("//"))
            {
                url = "http:" + url;
            }
            string id = node.SelectSingleNode("@id").Value;
            string page = "http://" + domain + "/index.php?page=post&s=view&id=" + id;

            return new string[]
            {
                url,
                page
            };
        }

        private async Task DAPICommandBase(CommandContext ctx,string name,string domain)
        {
            if (!ctx.IsNSFW())
            {
                await ctx.EmbedReply.Danger(ctx.Message, name, "Haha, did you really believe it would be that easy? :smirk:");
                return;
            }

            string[] result = await this.GetDAPIResult(ctx,domain);
            if(result.Length < 1)
            {
                await ctx.EmbedReply.Danger(ctx.Message, name, "Nothing was found");
                return;
            }
            
            string url = result[0];
            string page = result[1];

            Embed embed = this.CreateNSFWEmbed(ctx,name,url,page);

            await ctx.EmbedReply.Send(ctx.Message, embed);
        }

        [Command(Name="furrybooru",Help="Browses FurryBooru",Usage="furrybooru <tags|search>")]
        private async Task FurryBooru(CommandContext ctx)
        {
            await this.DAPICommandBase(ctx,"FurryBooru","furry.booru.org");
        }

        [Command(Name="r34",Help="Browses R34",Usage="r34 <tags|search>")]
        private async Task R34(CommandContext ctx)
        {
            await this.DAPICommandBase(ctx,"R34","rule34.xxx");
        }

        [Command(Name="gelbooru",Help="Browses GelBooru",Usage="gelbooru <tags|search>")]
        private async Task GelBooru(CommandContext ctx)
        {
            await this.DAPICommandBase(ctx,"GelBooru","gelbooru.com");
        }

        public void Initialize(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand(this.SearchE621);
            handler.LoadCommand(this.FurryBooru);
            handler.LoadCommand(this.R34);
            handler.LoadCommand(this.GelBooru);

            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}
