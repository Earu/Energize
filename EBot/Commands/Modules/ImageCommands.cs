using System;
using System.Collections.Generic;
using EBot.Logs;
using EBot.Utils;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;

namespace EBot.Commands.Modules
{
    class ImageCommands : ICommandModule
    {
        private string Name = "Image";

        private async Task Avatar(CommandContext ctx)
        {
            SocketUser user = ctx.Message.Author;
            if (!string.IsNullOrWhiteSpace(ctx.Arguments[0]))
            {
                IReadOnlyList<SocketUser> users = ctx.Message.MentionedUsers as IReadOnlyList<SocketUser>;
                user = users[0];
            }

            string url = user.GetAvatarUrl(ImageFormat.Png,512);
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithAuthor(ctx.Message.Author);
            builder.WithColor(new Color());
            builder.WithImageUrl(url);
            builder.WithFooter("Avatar");

            await ctx.EmbedReply.Send(ctx.Message, builder.Build());
        }

        private async Task BlackWhite(CommandContext ctx)
        {
            string url = string.IsNullOrWhiteSpace(ctx.Arguments[0]) ? ctx.LastPictureURL : ctx.Arguments[0];
            string path = null;

            if (!string.IsNullOrWhiteSpace(url)) //if getlastpicture returns nothing
            {
                path = await ImageProcess.DownloadImage(url);
            }

            if(path != null)
            {
                try
                {
                    ImageProcess.Resize(path);
                    ImageProcess.MakeBlackWhite(path);

                    await ctx.Message.Channel.SendFileAsync(path);

                    ImageProcess.DeleteImage(path);
                }catch(Exception e)
                {
                    BotLog.Debug(e.ToString());
                }
            }

            if(path == null)
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Ugh", "There's no valid url to use!");
            }
        }

        private async Task Wew(CommandContext ctx)
        {
            string url = string.IsNullOrWhiteSpace(ctx.Arguments[0]) ? ctx.LastPictureURL : ctx.Arguments[0];
            string path = null;
            string maskpath = "Masks/wew.png";

            if (!string.IsNullOrWhiteSpace(url)) //if getlastpicture returns nothing
            {
                path = await ImageProcess.DownloadImage(url);
            }

            if (path != null)
            {
                await ctx.EmbedReply.Good(ctx.Message, "WIP", "Sorry this command is under work!");
            }

            if (path == null)
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Ugh", "There's no valid url to use!");
            }
        }

        public void Load(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand("avatar", this.Avatar, "Get a user's avatar","avatar \"@user\"",this.Name);
            //this.Handler.LoadCommand("blackwhite", this.BlackWhite, "Make a picture black and white",this.Name);
            //this.Handler.LoadCommand("wew", this.Wew, "provide a picture to \"wew\" at");

            log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload(CommandHandler handler,BotLog log)
        {
            handler.UnloadCommand("avatar");
            //this.Handler.UnloadCommand("blackwhite");
            //this.Handler.UnloadCommand("wew");

            log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }
    }
}
