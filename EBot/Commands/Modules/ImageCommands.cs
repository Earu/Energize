using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.Entities;
using EBot.Logs;
using EBot.Commands.Utils;
using SixLabors.ImageSharp;
using EBotDiscord.Commands.Utils;

namespace EBot.Commands.Modules
{
    class ImageCommands
    {
        private string Name = "Image";
        private CommandsHandler Handler;
        private BotLog Log;

        public void Setup(CommandsHandler handler, BotLog log)
        {
            this.Handler = handler;
            this.Log = log;
        }

        private void Avatar(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            DiscordUser user = msg.Author;
            if (!string.IsNullOrWhiteSpace(args[0]))
            {
                user = msg.MentionedUsers[0];
            }

            string url = user.AvatarUrl;
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Color = new DiscordColor(110, 220, 110);
            embed.ImageUrl = url;
            embed.Title = "Avatar";

            embedrep.Send(msg, embed.Build());
        }

        private async void BlackWhite(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            string url = string.IsNullOrWhiteSpace(args[0]) ? Handler.GetLastPictureURL(msg.Channel) : args[0];
            string path = null;

            if (!string.IsNullOrWhiteSpace(url)) //if getlastpicture returns nothing
            {
                path = await ImageProcess.DownloadImage(url);
            }

            if(path != null)
            {
                ImageProcess.Resize(path);
                ImageProcess.MakeBlackWhite(path);

                await msg.Channel.SendFileAsync(path);

                ImageProcess.DeleteImage(path);
            }

            if(path == null)
            {
                embedrep.Danger(msg, "Ugh", "There's no valid url to use!");
            }
        }

        private async void Wew(CommandReplyEmbed embedrep,DiscordMessage msg,List<string> args)
        {
            string url = string.IsNullOrWhiteSpace(args[0]) ? Handler.GetLastPictureURL(msg.Channel) : args[0];
            string path = null;
            string maskpath = "Masks/wew.png";

            if (!string.IsNullOrWhiteSpace(url)) //if getlastpicture returns nothing
            {
                path = await ImageProcess.DownloadImage(url);
            }

            if (path != null)
            {
                Image<Rgba32> mask = Image.Load(maskpath);
                Image<Rgba32> provided = Image.Load(path);

                string basepath = await ImageProcess.Create(mask.Width, mask.Height);
                Image<Rgba32> img = Image.Load(basepath);

                Dictionary<string, ImagePoint> bounds = ImageProcess.GetBounds(maskpath);
                ImageProcess.Resize(path,bounds["Maxs"].X - bounds["Mins"].X, bounds["Maxs"].Y - bounds["Mins"].Y);

                int diffx = bounds["Mins"].X;
                int diffy = bounds["Mins"].Y;

                img[50, 50].PackFromRgba32(new Rgba32(0, 0, 0));
                img[50, 50].CreatePixelOperations();
                img.Save(basepath);

                await msg.Channel.SendFileAsync(basepath);

                ImageProcess.DeleteImage(path);
                ImageProcess.DeleteImage(basepath);
            }

            if (path == null)
            {
                embedrep.Danger(msg, "Ugh", "There's no valid url to use!");
            }
        }

        public void Load()
        {
            this.Handler.LoadCommand("avatar", this.Avatar, "Display your avatar or the avatar of the person you mentionned");
            this.Handler.LoadCommand("blackwhite", this.BlackWhite, "Make a picture black and white");
            //this.Handler.LoadCommand("wew", this.Wew, "provide a picture to \"wew\" at");

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload()
        {
            this.Handler.UnloadCommand("avatar");
            this.Handler.UnloadCommand("blackwhite");
            //this.Handler.UnloadCommand("wew");

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }
    }
}
