using Discord.WebSocket;
using EBot.Commands.Social;
using EBot.Logs;
using EBot.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EBot.Commands.Modules
{
    class SocialCommands : ICommandModule
    {
        private string Name = "Social";

        private async Task Hug(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            string result = "";
            IReadOnlyList<SocketUser> users = ctx.Message.MentionedUsers as IReadOnlyList<SocketUser>;
            if (!string.IsNullOrWhiteSpace(ctx.Arguments[0]) && users.Count > 0)
            {
                result = action.Hug(ctx.Message.Author, users);
                await ctx.EmbedReply.Good(ctx.Message, "Hug!", result);
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Aw", "You need to mention the persons you want to hug!");
            }

        }

        private async Task Boop(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            string result = "";
            IReadOnlyList<SocketUser> users = ctx.Message.MentionedUsers as IReadOnlyList<SocketUser>;
            if (!string.IsNullOrWhiteSpace(ctx.Arguments[0]) && users.Count > 0)
            {
                result = action.Boop(ctx.Message.Author, users);
                await ctx.EmbedReply.Good(ctx.Message, "Boop!", result);
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Aw", "You need to mention the persons you want to boop!");
            }

        }

        private async Task Slap(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            string result = "";
            IReadOnlyList<SocketUser> users = ctx.Message.MentionedUsers as IReadOnlyList<SocketUser>;
            if (!string.IsNullOrWhiteSpace(ctx.Arguments[0]) && users.Count > 0)
            {
                result = action.Slap(ctx.Message.Author, (ctx.Message.MentionedUsers as IReadOnlyList<SocketUser>));
                await ctx.EmbedReply.Good(ctx.Message, "Slap!", result);
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Aw", "You need to mention the persons you want to slap!");
            }

        }

        private async Task Love(CommandContext ctx)
        {
            string endpoint = "https://love-calculator.p.mashape.com/getPercentage?";
            if(ctx.Message.MentionedUsers.Count > 0 && ctx.Message.MentionedUsers.Count < 3)
            {
                IReadOnlyList<SocketUser> users = ctx.Message.MentionedUsers as IReadOnlyList<SocketUser>;
                endpoint += "fname=" + users[0].Username + "&";
                endpoint += "sname=" + users[1].Username;

                string json = await HTTP.Fetch(endpoint, ctx.Log, null, req =>
                {
                    req.Headers[System.Net.HttpRequestHeader.Accept] = "text/plain";
                    req.Headers["X-Mashape-Key"] = EBotCredentials.MASHAPE_KEY;
                });

                LoveObject love = JSON.Deserialize<LoveObject>(json, ctx.Log);

                await ctx.EmbedReply.Good(ctx.Message, "Love", ":heartbeat: \t" + love.percentage + "%\n" + love.result);
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Erm", "You need to mention two persons!");
            }
        }

        public void Load(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand(new string[]{ "hug","hugs"}, this.Hug, "Hugs a person","hug \"@user1\",\"@user2\",\"@user3\",...", this.Name);
            handler.LoadCommand("boop", this.Boop, "Boops a person","boop \"@user1\",\"@user2\",\"@user3\",...", this.Name);
            handler.LoadCommand("slap", this.Slap, "Slaps a person","slap \"@user1\",\"@user2\",\"@user3\",...", this.Name);
            handler.LoadCommand("love", this.Love, "Gets love stats between two users", "love \"@user1\" \"@user2\"", this.Name);

            log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload(CommandHandler handler,BotLog log)
        {
            handler.UnloadCommand("hug");
            handler.UnloadCommand("boop");
            handler.UnloadCommand("slap");
            handler.UnloadCommand("love");

            log.Nice("Module", ConsoleColor.Green, "Unloaded " + this.Name);
        }
    }
}
