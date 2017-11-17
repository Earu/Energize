using System;
using System.Collections.Generic;
using DSharpPlus.Entities;
using EBot.Logs;
using EBot.Utils;
using System.Threading.Tasks;

namespace EBot.Commands.Modules
{
    class ImageCommands : ICommandModule
    {
        private string Name = "Image";
        private CommandsHandler Handler;
        private BotLog Log;

        public void Setup(CommandsHandler handler, BotLog log)
        {
            this.Handler = handler;
            this.Log = log;
        }

        private async Task Avatar(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            DiscordUser user = msg.Author;
            if (!string.IsNullOrWhiteSpace(args[0]))
            {
                user = msg.MentionedUsers[0];
            }

            string url = user.AvatarUrl;
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(),
                ImageUrl = url,
                Title = "Avatar - " + msg.Author.Username
            };

            await embedrep.Send(msg, embed.Build());
        }

        private async Task BlackWhite(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            string url = string.IsNullOrWhiteSpace(args[0]) ? Handler.GetLastPictureURL(msg.Channel) : args[0];
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

                    await msg.Channel.SendFileAsync(path);

                    ImageProcess.DeleteImage(path);
                }catch(Exception e)
                {
                    BotLog.Debug(e.ToString());
                }
            }

            if(path == null)
            {
                await embedrep.Danger(msg, "Ugh", "There's no valid url to use!");
            }
        }

        private async Task Wew(CommandReplyEmbed embedrep,DiscordMessage msg,List<string> args)
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
                await embedrep.Good(msg, "WIP", "Sorry this command is under work!");
            }

            if (path == null)
            {
                await embedrep.Danger(msg, "Ugh", "There's no valid url to use!");
            }
        }

        public void Load()
        {
            this.Handler.LoadCommand("avatar", this.Avatar, "Get a user's avatar","avatar \"@user\"",this.Name);
            //this.Handler.LoadCommand("blackwhite", this.BlackWhite, "Make a picture black and white",this.Name);
            //this.Handler.LoadCommand("wew", this.Wew, "provide a picture to \"wew\" at");

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload()
        {
            this.Handler.UnloadCommand("avatar");
            //this.Handler.UnloadCommand("blackwhite");
            //this.Handler.UnloadCommand("wew");

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }
    }
}
