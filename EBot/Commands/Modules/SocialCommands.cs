using Discord.WebSocket;
using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EBot.Commands.Modules
{
    class SocialCommands : ICommandModule
    {
        private string Name = "Social";
        private CommandsHandler Handler;
        private BotLog Log;

        public void Setup(CommandsHandler handler, BotLog log)
        {
            this.Handler = handler;
            this.Log = log;
        }

        private async Task Hug(CommandReplyEmbed embedrep, SocketMessage msg, List<string> args)
        {
            Social.Action action = new Social.Action();
            string result = "";
            IReadOnlyList<SocketUser> users = msg.MentionedUsers as IReadOnlyList<SocketUser>;
            if (!string.IsNullOrWhiteSpace(args[0]) && users.Count > 0)
            {
                result = action.Hug(msg.Author, users);
                await embedrep.Good(msg, "Hug!", result);
            }
            else
            {
                await embedrep.Danger(msg, "Aw", "You need to mention the persons you want to hug!");
            }

        }

        private async Task Boop(CommandReplyEmbed embedrep, SocketMessage msg, List<string> args)
        {
            Social.Action action = new Social.Action();
            string result = "";
            IReadOnlyList<SocketUser> users = msg.MentionedUsers as IReadOnlyList<SocketUser>;
            if (!string.IsNullOrWhiteSpace(args[0]) && users.Count > 0)
            {
                result = action.Boop(msg.Author, users);
                await embedrep.Good(msg, "Boop!", result);
            }
            else
            {
                await embedrep.Danger(msg, "Aw", "You need to mention the persons you want to boop!");
            }

        }

        private async Task Slap(CommandReplyEmbed embedrep, SocketMessage msg, List<string> args)
        {
            Social.Action action = new Social.Action();
            string result = "";
            IReadOnlyList<SocketUser> users = msg.MentionedUsers as IReadOnlyList<SocketUser>;
            if (!string.IsNullOrWhiteSpace(args[0]) && users.Count > 0)
            {
                result = action.Slap(msg.Author, (msg.MentionedUsers as IReadOnlyList<SocketUser>));
                await embedrep.Good(msg, "Slap!", result);
            }
            else
            {
                await embedrep.Danger(msg, "Aw", "You need to mention the persons you want to slap!");
            }

        }

        public void Load()
        {
            this.Handler.LoadCommand(new string[]{ "hug","hugs"}, this.Hug, "Hugs a person","hug \"@user1\",\"@user2\",\"@user3\",...", this.Name);
            this.Handler.LoadCommand("boop", this.Boop, "Boops a person","boop \"@user1\",\"@user2\",\"@user3\",...", this.Name);
            this.Handler.LoadCommand("slap", this.Slap, "Slaps a person","slap \"@user1\",\"@user2\",\"@user3\",...", this.Name);

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload()
        {
            this.Handler.UnloadCommand("hug");
            this.Handler.UnloadCommand("boop");
            this.Handler.UnloadCommand("slap");

            this.Log.Nice("Module", ConsoleColor.Green, "Unloaded " + this.Name);
        }
    }
}
