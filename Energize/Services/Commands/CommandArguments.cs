using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Energize.Services.Commands
{
    public class CommandArguments
    {
        [CommandArg("me")]
        public SocketUser GetAuthor(CommandContext ctx,List<string> args) => ctx.Message.Author;

        [CommandArg("last")]
        public SocketUser GetLast(CommandContext ctx,List<string> args) => ctx.Cache.LastMessage.Author;

        [CommandArg("random")]
        public SocketUser GetRandom(CommandContext ctx,List<string> args)
        {
            Random rand = new Random();
            if (ctx.IsPrivate)
                return rand.Next(0, 1) == 1 ? ctx.Message.Author : ctx.Client.CurrentUser as SocketUser;
            else
            {
                int index = rand.Next(0, ctx.GuildCachedUsers.Count - 1);
                return ctx.GuildCachedUsers[index] as SocketUser;
            }
        }

        [CommandArg("admin")]
        public SocketUser GetRandomAdmin(CommandContext ctx,List<string> args)
        {
            Random rand = new Random();
            if (ctx.IsPrivate)
            {
                return rand.Next(0, 1) == 1 ? ctx.Message.Author : ctx.Client.CurrentUser as SocketUser;
            }
            else
            {
                List<SocketGuildUser> admins = ctx.GuildCachedUsers.Where(x => x.GuildPermissions.Administrator).ToList();
                return admins[rand.Next(0, admins.Count - 1)] as SocketUser;
            }
        }

        [CommandArg("r")]
        public SocketUser GetByRole(CommandContext ctx,List<string> args)
        {
            if (ctx.IsPrivate) return null;
            if (args.Count < 1) return null;

            string role = args[0].ToLower();
            List<SocketGuildUser> results = ctx.GuildCachedUsers.Where(x => x.Roles.Any(r => r.Name.ToLower().Contains(role))).ToList();
            if (results.Count == 0)
            {
                return null;
            }
            else
            {
                Random rand = new Random();
                return results[rand.Next(0, results.Count - 1)] as SocketUser;
            }
        }

        [CommandArg("n")]
        public SocketUser GetByExactName(CommandContext ctx,List<string> args)
        {
            if (args.Count < 1) return null;

            string name = args[0].ToLower();
            if (ctx.IsPrivate)
            {
                if (ctx.Message.Author.Username.ToLower() == name)
                    return ctx.Message.Author;
                else if (ctx.Client.CurrentUser.Username.ToLower() == name)
                    return ctx.Client.CurrentUser as SocketUser;
                else
                    return null;
            }
            else
            {
                List<SocketGuildUser> results = ctx.GuildCachedUsers.Where(x => x.Username != null && x.Username.ToLower() == name).ToList();
                if (results.Count == 0)
                {
                    return null;
                }
                else
                {
                    Random rand = new Random();
                    return results[rand.Next(0, results.Count - 1)] as SocketUser;
                }
            }
        }

        [CommandArg("id")]
        public SocketUser GetByID(CommandContext ctx,List<string> args)
        {
            if (args.Count < 1) return null;

            if (ulong.TryParse(args[0], out ulong id))
            {
                if (ctx.IsPrivate)
                {
                    if (ctx.Message.Author.Id == id)
                        return ctx.Message.Author;
                    else if (ctx.Client.CurrentUser.Id == id)
                        return ctx.Client.CurrentUser as SocketUser;
                    else
                        return null;
                }
                else
                {
                    List<SocketGuildUser> results = ctx.GuildCachedUsers.Where(x => x.Id == id).ToList();
                    if (results.Count == 0)
                    {
                        return null;
                    }
                    else
                    {
                        Random rand = new Random();
                        return results[rand.Next(0, results.Count - 1)] as SocketUser;
                    }
                }
            }
            else
            {
                return null;
            }
        }
    }
}
