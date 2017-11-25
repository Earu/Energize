using System;
using EBot.Logs;
using EBot.Utils;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;

namespace EBot.Commands.Modules
{
    [CommandModule(Name="Image")]
    class ImageCommands : CommandModule,ICommandModule
    {
        [Command(Name="avatar",Help="Gets the avatar of a user",Usage="avatar <@user|nothing>")]
        private async Task Avatar(CommandContext ctx)
        {
            SocketUser user = ctx.Message.Author;
            if (ctx.TryGetUser(ctx.Arguments[0],out SocketUser u))
            {
                user = u;
            }

            string url = user.GetAvatarUrl(ImageFormat.Png,512);
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithAuthor(ctx.Message.Author);
            builder.WithColor(ctx.EmbedReply.ColorGood);
            builder.WithImageUrl(url);
            builder.WithFooter("Avatar");

            ctx.EmbedReply.Send(ctx.Message, builder.Build());
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
                ctx.EmbedReply.Danger(ctx.Message, "Ugh", "There's no valid url to use!");
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
                ctx.EmbedReply.Good(ctx.Message, "WIP", "Sorry this command is under work!");
            }

            if (path == null)
            {
                ctx.EmbedReply.Danger(ctx.Message, "Ugh", "There's no valid url to use!");
            }
        }

        public void Initialize(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand(this.Avatar);
            //this.Handler.LoadCommand("blackwhite", this.BlackWhite, "Make a picture black and white",this.Name);
            //this.Handler.LoadCommand("wew", this.Wew, "provide a picture to \"wew\" at");

            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}
