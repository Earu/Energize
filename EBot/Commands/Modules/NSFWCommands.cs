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
        private CommandsHandler Handler;
        private BotLog Log;

        public void Setup(CommandsHandler handler, BotLog log)
        {
            this.Handler = handler;
            this.Log = log;
        }

        private async Task SearchE621(CommandReplyEmbed embedrep, SocketMessage msg, List<string> args)
        {
            if (msg.Channel.IsNsfw)
            {
                Random rand = new Random();
                string body = await HTTP.Fetch("https://e621.net/post/index.json",this.Log);
                List<E621.EPost> posts = JSON.Deserialize<List<E621.EPost>>(body,this.Log);
                E621.EPost post;

                if(posts == null)
                {
                    await embedrep.Danger(msg, "Err", "There was no data to use sorry!");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(args[0]))
                    {
                        post = E621.SortingHandler.GetRandom(posts);
                    }
                    else
                    {
                        post = E621.Search.Handle(posts, args);
                    }

                    if (post == null)
                    {
                        await embedrep.Danger(msg, "Nooo", "Seems like I couldn't find anything!");
                    }
                    else
                    {
                        EmbedBuilder embed = new EmbedBuilder
                        {
                            Color = new Color(110, 220, 110),
                            ImageUrl = post.sample_url,
                            Title = "E621 - " + msg.Author.Username,
                            Description = post.sample_url + "\n*Width: " + post.sample_width + "\tHeight: " + post.sample_height + "*"
                        };

                        await embedrep.Send(msg, embed.Build());
                    }
                }
            }
            else
            {
                await embedrep.Danger(msg, "Hum no.", "Haha, did you really believe it would be that easy? :smirk:");
            }
        }

        public void Load()
        {
            this.Handler.LoadCommand("e621", this.SearchE621, "Browse e621","e621 \"search\"",this.Name);

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload()
        {
            this.Handler.UnloadCommand("e621");

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }
    }
}
