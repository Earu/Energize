using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EBot.Commands.Social
{
    class Action
    {
        private static string[] _Hugs =
        {
            "<origin> comes up close to <user> and <action>s them",
            "<origin> sneaks behind <user> and <action>s them",
            "<origin> gives <user> a warm friendly <action>"
        };

        private static string[] _Boops =
        {
            "<user> got <action>ed by <origin>",
            "<origin> proceeds to gently <action> <user>",
            "<origin> ropes down toward <user> and sneakily <action>s them"
        };

        private static string[] _Slaps =
        {
            "<origin> <action>s <user> booty hard",
            "<origin> gets mad and <action>s <user> in the face",
            "<origin> <action>s <user> into another dimension!"
        };

        private string Global(DiscordUser from, IReadOnlyList<DiscordUser> to, string act, string[] sentences)
        {
            Random rand = new Random();
            string action = sentences[rand.Next(0, sentences.Length)];
            action = action.Replace("<origin>",from.Mention);
            action = action.Replace("<action>", act);
            string users = "";
            foreach(DiscordUser user in to)
            {
                users += user.Mention + " and ";
            }
            users = users.Remove(users.Length - 4);
            action = action.Replace("<user>", users);

            return action;
        }

        public string Hug(DiscordUser from,IReadOnlyList<DiscordUser> to)
        {
            return Global(from, to, "hug",_Hugs);
        }

        public string Boop(DiscordUser from, IReadOnlyList<DiscordUser> to)
        {
            return Global(from, to, "boop",_Boops);
        }

        public string Slap(DiscordUser from, IReadOnlyList<DiscordUser> to)
        {
            return Global(from, to, "slap",_Slaps);
        }
    }
}
