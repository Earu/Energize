using Discord.WebSocket;
using Energize.Services.Commands.Social;
using Energize.Services.Database;
using Energize.Services.Database.Models;
using Energize.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Services.Commands.Modules
{
    [CommandModule("Social")]
    class SocialCommands
    {
        private delegate Task<string> ActionCallback(SocketUser from, IReadOnlyList<SocketUser> to);

        private async Task Action(CommandContext ctx,string what,ActionCallback callback)
        {
            string result = "";
            List<SocketUser> users = new List<SocketUser>();
            foreach (string input in ctx.Arguments)
            {
                if (ctx.TryGetUser(input, out SocketUser u))
                {
                    users.Add(u);
                }
            }

            if (users.Count > 0)
            {
                result = await callback(ctx.Message.Author, users);
                await ctx.MessageSender.Good(ctx.Message, what, result);
            }
            else
            {
                await ctx.SendBadUsage();
            }
        }

        [Command(Name="hug",Help="Hugs people",Usage="hug <@user>,<@user|nothing>,...")]
        private async Task Hug(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            await this.Action(ctx, "Hug", action.Hug);
        }

        [Command(Name = "boop", Help = "Boops people", Usage = "boop <@user>,<@user|nothing>,...")]
        private async Task Boop(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            await this.Action(ctx, "Boop", action.Boop);
        }

        [Command(Name = "slap", Help = "Slaps people", Usage = "slap <@user>,<@user|nothing>,...")]
        private async Task Slap(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            await this.Action(ctx, "Slap", action.Slap);
        }

        [Command(Name = "kiss", Help = "Kisses people", Usage = "kiss <@user>,<@user|nothing>,...")]
        private async Task Kiss(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            await this.Action(ctx, "Kiss", action.Kiss);
        }

        [Command(Name = "snuggle", Help = "Snuggles people", Usage = "snuggle <@user>,<@user|nothing>,...")]
        private async Task Snuggle(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            await this.Action(ctx, "Snuggle", action.Snuggle);
        }

        [Command(Name = "shoot",Help = "Shoots people", Usage = "shoot <@user>,<@user|nothing>,...")]
        private async Task Shoot(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            await this.Action(ctx, "Shoot", action.Shoot);
        }

        [Command(Name = "pet", Help = "Pets people", Usage = "pet <@user>,<@user|nothing>,...")]
        private async Task Pet(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            await this.Action(ctx, "Pet", action.Pet);
        }

        [Command(Name = "spank", Help = "Spanks people", Usage = "spank <@user>,<@user|nothing>,...")]
        private async Task Spank(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            await this.Action(ctx, "Spank", action.Spank);
        }

        [Command(Name = "yiff", Help = "Yiffs people", Usage = "yiff <@user>,<@user|nothing>,...")]
        private async Task Yiff(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            await this.Action(ctx, "Yiff", action.Yiff);
        }

        [Command(Name="nom",Help="Nibbles people",Usage="nom <@user>,<@user|nothing>,...")]
        private async Task Nom(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            await this.Action(ctx,"Nom",action.Nom);
        }

        [Command(Name="lick",Help="Licks people",Usage="nom <@user>,<@user|nothing>,...")]
        private async Task Lick(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            await this.Action(ctx,"Lick",action.Lick);
        }

        [Command(Name="bite",Help="Bites people",Usage="bite <@user>,<@user|nothing>,...")]
        private async Task Bite(CommandContext ctx)
        {
            Social.Action action = new Social.Action();
            await this.Action(ctx,"Bite",action.Bite);
        }

        [Command(Name="love",Help="Gets a love percentage between two users",Usage="love <@user>,<@user>")]
        private async Task Love(CommandContext ctx)
        {
            string endpoint = "https://love-calculator.p.mashape.com/getPercentage?";
            if(ctx.TryGetUser(ctx.Arguments[0],out SocketUser u1) && ctx.TryGetUser(ctx.Arguments[1],out SocketUser u2))
            {
                endpoint += "fname=" + u1.Username + "&";
                endpoint += "sname=" + u2.Username;

                string json = await HTTP.Fetch(endpoint, ctx.Log, null, req =>
                {
                    req.Headers[System.Net.HttpRequestHeader.Accept] = "text/plain";
                    req.Headers["X-Mashape-Key"] = EnergizeConfig.MASHAPE_KEY;
                });

                LoveObject love = JSON.Deserialize<LoveObject>(json, ctx.Log);

                await ctx.MessageSender.Good(ctx.Message, "Love", u1.Mention + " & " + u2.Mention 
                    + "\n:heartbeat: \t" + love.percentage + "%\n" + love.result);
            }
            else
            {
                await ctx.SendBadUsage();
            }
        }

        [Command(Name="setdesc",Help="Sets your description",Usage="setdesc <description>")]
        private async Task SetDescription(CommandContext ctx)
        {
            if(!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
                return;
            }

            DBContextPool db = ServiceManager.GetService<DBContextPool>("Database");
            using (DBContext dbctx = await db.GetContext())
            {
                DiscordUser resuser = await dbctx.Instance.GetOrCreateUser(ctx.Message.Author.Id);
                resuser.Description = ctx.Input;
            }

            await ctx.MessageSender.Good(ctx.Message, "Description", "Description successfully changed");
        }

        [Command(Name="desc",Help="Show another user's description",Usage="desc <@user>")]
        private async Task Description(CommandContext ctx)
        {
            if(!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
                return;
            }

            if(ctx.TryGetUser(ctx.Arguments[0],out SocketUser user))
            {
                DBContextPool db = ServiceManager.GetService<DBContextPool>("Database");
                using (DBContext dbctx = await db.GetContext())
                {
                    DiscordUser resuser = await dbctx.Instance.GetOrCreateUser(user.Id);
                    await ctx.MessageSender.Good(ctx.Message, "Description", $"{user.Mention}'s description is:\n`{resuser.Description}`");
                }
            }
            else
            {
                await ctx.MessageSender.Danger(ctx.Message, "Description", "Coulnd't find any user for your input");
            }
        }

        [Command(Name="stats",Help="Gets the social stats of a user",Usage="stats <@user|userid>")]
        private async Task Stats(CommandContext ctx)
        {
            if(!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
                return;
            }

            if(ctx.TryGetUser(ctx.Arguments[0],out SocketUser user,true))
            {
                DBContextPool db = ServiceManager.GetService<DBContextPool>("Database");
                using (DBContext dbctx = await db.GetContext())
                {
                    DiscordUserStats s = await dbctx.Instance.GetOrCreateUserStats(user.Id);
                    if(s == null)
                    {
                        await ctx.MessageSender.Good(ctx.Message,"Social Stats","This user didn't interact with anybody yet");
                    }
                    else
                    {
                        string res = $"{user.Mention} got:\n***HUGGED:*** `{s.HuggedCount}`\t***KISSED:*** `{s.KissedCount}`\n"
                            + $"***SNUGGLED:*** `{s.SnuggledCount}`\t***PET:*** `{s.PetCount}`\n"
                            + $"***NIBBLED:*** `{s.NomedCount}`\t***SPANKED:*** `{s.SpankedCount}`\n"
                            + $"***SHOT:*** `{s.ShotCount}`\t***SLAPPED:*** `{s.SlappedCount}`\n"
                            + $"***YIFFED:*** `{s.YiffedCount}`\t***BITTEN:*** `{s.BittenCount}`\n"
                            + $"***BOOPED:*** `{s.BoopedCount}`";

                        await ctx.MessageSender.Good(ctx.Message, "Social Stats", res);
                    }
                }
            }
            else
            {
                await ctx.MessageSender.Danger(ctx.Message,"Social Stats","Couldn't find any user for your input");
            }
        }
    }
}
