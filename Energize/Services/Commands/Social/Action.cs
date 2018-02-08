using Discord.WebSocket;
using Energize.Services.Database;
using Energize.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Services.Commands.Social
{
    class Action
    {
        private delegate void DBCountCallback(DiscordUserStats stats);
        private Dictionary<string, DBCountCallback> _DBCountCallbacks = new Dictionary<string, DBCountCallback>
        {
            ["hug"] = s => s.HuggedCount++,
            ["boop"] = s => s.BoopedCount++,
            ["slap"] = s => s.SlappedCount++,
            ["kiss"] = s => s.KissedCount++,
            ["snuggle"] = s => s.SnuggledCount++,
            ["shoot"] = s => s.ShotCount++,
            ["pet"] = s => s.PetCount++,
            ["spank"] = s => s.SpankedCount++,
            ["yiff"] = s => s.YiffedCount++,
            ["nom"] = s => s.NomedCount++ ,
            ["lick"] = s => s.LickedCount++,
            ["bite"] = s => s.BittenCount++,
        };

        private async Task<string> Global(SocketUser from, IReadOnlyList<SocketUser> to, string act, string[] sentences)
        {
            Random rand = new Random();
            string action = sentences[rand.Next(0, sentences.Length)];
            action = action.Replace("<origin>",from.Mention);
            action = action.Replace("<action>", act);
            
            int count = 0;
            string users = "";
            foreach (SocketUser user in to)
            {
                DBContextPool db = ServiceManager.GetService<DBContextPool>("Database");
                using (DBContext ctx = await db.GetContext())
                {
                    DiscordUser dbuser = await ctx.Context.GetOrCreateUser(user.Id);
                    dbuser.Stats = dbuser.Stats ?? new DiscordUserStats();
                    _DBCountCallbacks[act](dbuser.Stats);
                }
                users += user.Mention + " and ";
                count++;

                if (count > 3) break;
            }

            users = users.Remove(users.Length - 4);
            action = action.Replace("<user>", users);

            return action;
        }

        public async Task<string> Hug(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return await Global(from, to, "hug",new string[]
            {
                "<origin> comes up close to <user> and <action>s them",
                "<origin> sneaks behind <user> and <action>s them",
                "<origin> gives <user> a warm friendly <action>"
            });
        }

        public async Task<string> Boop(SocketUser from, IReadOnlyList<SocketUser> to)
        {
            return await Global(from, to, "boop",new string[]
            {
                "<user> got <action>ed by <origin>",
                "<origin> proceeds to gently <action> <user>",
                "<origin> ropes down toward <user> and sneakily <action>s them"
            });
        }

        public async Task<string> Slap(SocketUser from, IReadOnlyList<SocketUser> to)
        {
            return await Global(from, to, "slap",new string[]
            {
                "<origin> <action>s <user> angrily",
                "<origin> gets mad and <action>s <user> in the face",
                "<origin> <action>s <user> into another dimension!"
            });
        }

        public async Task<string> Kiss(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return await Global(from, to, "kiss",new string[]
            {
                "<origin> gives a big <action> to <user>",
                "<origin> gently <action>es <user>",
                "<origin> <action>es <user>"
            });
        }

        public async Task<string> Snuggle(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return await Global(from, to, "snuggle",new string[]
            {
                "<origin> <action>s against <user>",
                "<origin> <action>s <user>",
                "<origin> lovely <action>s <user>"
            });
        }

        public async Task<string> Shoot(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return await Global(from, to, "shoot", new string[]
            {
                "<origin> <action>s at <user>",
                "<origin> pulls out a gun and <action>s <user>",
                "<origin> takes out a rifle and <action>s <user>"
            });
        }

        public async Task<string> Pet(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return await Global(from, to, "pet", new string[]
            {
                "<origin> comes up to <user> and <action>s their head(s)",
                "<origin> gives <user> a gentle <action>",
                "<origin> gets close to <user> and proceeds to <action> them"
            });
        }

        public async Task<string> Spank(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return await Global(from, to, "spank", new string[]
            {
                "<origin> gives a large <action> to <user>",
                "<origin> <action>s <user>, hard.",
                "<origin> <action>s in a kinky way <user>"
            });
        }

        public async Task<string> Yiff(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return await Global(from, to, "yiff", new string[]
            {
                "<origin> <action>s <user> *cringe*",
                "<origin> wildy <action>s <user> *cringe*",
                "With a lack of self-esteem <origin> <action>s <user> *cringe*"
            });
        }

        public async Task<string> Nom(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return await Global(from,to,"nom",new string[]
            {
                "<origin> <action>s <user>",
                "<origin> gently <action>s <user>",
                "<user> just have been <action>ed by <origin>"
            });
        }

        public async Task<string> Lick(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return await Global(from,to,"lick",new string[]
            {
                "<origin> softly <action>s <user>",
                "<origin> <action>s <user> across their faces",
                "<origin> gives a big wet <action> to <user>"
            });
        }

        public async Task<string> Bite(SocketUser from,IReadOnlyList<SocketUser> to)
        {
            return await Global(from,to,"bite",new string[]
            {
                "<origin> angrily <action>s <user>",
                "<origin> <action>s <user> hard",
                "With all their strength <origin> <action>s <user>"
            });
        }
    }
}
