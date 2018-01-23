using Discord.WebSocket;
using System;
using System.Collections.Generic;

namespace Energize.Commands.Social
{
    class Action
    {
        private string Global(SocketUser from, IReadOnlyList<SocketUser> to, string act, string[] sentences)
        {
            Random rand = new Random();
            string action = sentences[rand.Next(0, sentences.Length)];
            action = action.Replace("<origin>",from.Mention);
            action = action.Replace("<action>", act);
            
            int count = 0;
            string users = "";
            foreach(SocketUser user in to)
            {
                users += user.Mention + " and ";
                count++;
                
                if(count > 3) break;
            }
            users = users.Remove(users.Length - 4);
            action = action.Replace("<user>", users);

            return action;
        }

        public string Hug(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return Global(from, to, "hug",new string[]
            {
                "<origin> comes up close to <user> and <action>s them",
                "<origin> sneaks behind <user> and <action>s them",
                "<origin> gives <user> a warm friendly <action>"
            });
        }

        public string Boop(SocketUser from, IReadOnlyList<SocketUser> to)
        {
            return Global(from, to, "boop",new string[]
            {
                "<user> got <action>ed by <origin>",
                "<origin> proceeds to gently <action> <user>",
                "<origin> ropes down toward <user> and sneakily <action>s them"
            });
        }

        public string Slap(SocketUser from, IReadOnlyList<SocketUser> to)
        {
            return Global(from, to, "slap",new string[]
            {
                "<origin> <action>s <user> angrily",
                "<origin> gets mad and <action>s <user> in the face",
                "<origin> <action>s <user> into another dimension!"
            });
        }

        public string Kiss(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return Global(from, to, "kiss",new string[]
            {
                "<origin> gives a big <action> to <user>",
                "<origin> gently <action>es <user>",
                "<origin> <action>es <user>"
            });
        }

        public string Snuggle(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return Global(from, to, "snuggle",new string[]
            {
                "<origin> <action>s against <user>",
                "<origin> <action>s <user>",
                "<origin> lovely <action>s <user>"
            });
        }

        public string Shoot(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return Global(from, to, "shoot", new string[]
            {
                "<origin> <action>s at <user>",
                "<origin> pulls out a gun and <action>s <user>",
                "<origin> takes out a rifle and <action>s <user>"
            });
        }

        public string Pet(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return Global(from, to, "pet", new string[]
            {
                "<origin> comes up to <user> and <action>s their head(s)",
                "<origin> gives <user> a gentle <action>",
                "<origin> gets close to <user> and proceeds to <action> them"
            });
        }

        public string Spank(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return Global(from, to, "spank", new string[]
            {
                "<origin> gives a large <action> to <user>",
                "<origin> <action>s <user>, hard.",
                "<origin> <action>s in a kinky way <user>"
            });
        }

        public string Yiff(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return Global(from, to, "yiff", new string[]
            {
                "<origin> <action>s <user> *cringe*",
                "<origin> wildy <action>s <user> *cringe*",
                "With a lack of self-esteem <origin> <action>s <user> *cringe*"
            });
        }

        public string Nom(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return Global(from,to,"nom",new string[]
            {
                "<origin> <action>s <user>",
                "<origin> gently <action>s <user>",
                "<user> just have been <action>ed by <origin>"
            });
        }

        public string Lick(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return Global(from,to,"lick",new string[]
            {
                "<origin> softly <action>s <user>",
                "<origin> <action>s <user> across their faces",
                "<origin> gives a big wet <action> to <user>"
            });
        }

        public string Bite(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return Global(from,to,"bite",new string[]
            {
                "<origin> angrily <action>s <user>",
                "<origin> <action>s <user> hard",
                "With all their strength <origin> <action>s <user>"
            });
        }
    }
}
