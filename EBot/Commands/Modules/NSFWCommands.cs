using DSharpPlus.Entities;
using EBot.Commands.Utils;
using EBot.Logs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EBot.Commands.Modules
{
    class NSFWCommands
    {
        private string Name = "NSFW";
        private CommandsHandler Handler;
        private BotLog Log;

        public void Setup(CommandsHandler handler, BotLog log)
        {
            this.Handler = handler;
            this.Log = log;
        }

        private async void SearchE621(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            if (msg.Channel.IsNSFW)
            {
                Random rand = new Random();
                string body = await HTTP.Fetch("https://e621.net/post/index.json",this.Log);
                List<E621.PostObject> posts = JsonConvert.DeserializeObject<List<E621.PostObject>>(body);
                E621.PostObject post;

                if (string.IsNullOrWhiteSpace(args[0]))
                {
                    post = E621.SortingHandler.GetRandom(posts);
                }
                else
                {
                    post = E621.Search.Handle(posts, args);
                }

                if (post is null)
                {
                    embedrep.Danger(msg, "Nooo", "Seems like I couldn't find anything!");
                }
                else
                {
                    DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                    embed.Color = new DiscordColor(110, 220, 110);
                    embed.ImageUrl = post.sample_url;
                    embed.Title = "E621";
                    embed.Description = post.sample_url + "\n*Width: " + post.sample_width + "\tHeight: " + post.sample_height + "*";

                    embedrep.Send(msg, embed.Build());
                }
            }
            else
            {
                embedrep.Danger(msg, "Hum no.", "Haha, did you really believe it would be that easy? :smirk:");
            }
        }

        private void RandNSFW(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            List<string> cache = this.Handler.GetURLCache("nsfw");
            Random rand = new Random();
            if (msg.Channel.IsNSFW)
            {
                if (cache.Count == 0)
                {
                    embedrep.Danger(msg, "Aaah", "Looks like I haven't saved anything yet!");
                }
                else
                {
                    string url = cache[rand.Next(0, cache.Count)];
                    DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                    embed.Color = new DiscordColor(110, 220, 110);
                    embed.ImageUrl = url;
                    embed.Title = "RandNSFW";

                    embedrep.Send(msg, embed.Build());
                }
            }
            else
            {
                embedrep.Danger(msg, "Hum no.", "Haha, did you really believe it would be that easy? :smirk:");
            }
        }

        public void Load()
        {
            this.Handler.LoadCommand("randnsfw", this.RandNSFW, "[NSFW] Display one of the many nsfw image saved");
            this.Handler.LoadCommand("e621", this.SearchE621, "[NSFW] Search pictures on e621");

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload()
        {
            this.Handler.UnloadCommand("randnsfw");
            this.Handler.UnloadCommand("e621");

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }
    }
}
