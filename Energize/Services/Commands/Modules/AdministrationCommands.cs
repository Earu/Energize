using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energize.Services.MemoryStream;
using Energize.Services.Database;
using Energize.Services.Database.Models;
using Energize.Services.Listeners;

namespace Energize.Services.Commands.Modules
{
    [CommandModule("Administration")]
    class AdministrationCommands
    {

        private async Task Role(CommandContext ctx, string name, Action<SocketGuildUser> callback)
        {
            if (!(ctx.Message.Channel is IGuildChannel))
            {
                await ctx.MessageSender.Danger(ctx.Message, name, "You can't do that in a DM channel");
                return;
            }

            SocketGuildUser user = ctx.Message.Author as SocketGuildUser;
            if (!user.GuildPermissions.Administrator) //&& user.Id != EnergizeConfig.OWNER_ID) debug
            {
                await ctx.MessageSender.Danger(ctx.Message, name, "Only a server administrator can do that");
                return;
            }

            if (ctx.TryGetUser(ctx.Arguments[0], out SocketUser u))
            {
                if (u is SocketGuildUser)
                {
                    SocketGuildUser gu = u as SocketGuildUser;
                    callback(gu);
                }
                else
                {
                    await ctx.MessageSender.Danger(ctx.Message, name, "This user is not part of the server!");
                }
            }
            else
            {
                await ctx.MessageSender.Danger(ctx.Message, name, "No user was found for your input");
            }

        }

        [Command(Name = "op", Help = "Makes a user able to use administration commands", Usage = "op <user>")]
        private async Task OP(CommandContext ctx)
        {
            await this.Role(ctx, "OP", async user =>
             {
                 try
                 {
                     IRole role = await ctx.GetOrCreateRole(user, "EnergizeAdmin");

                     await user.AddRoleAsync(role);
                     await ctx.MessageSender.Good(ctx.Message, "OP", user.Username + "#" + user.Discriminator + " was succesfully allowed to use administration commands");
                 }
                 catch
                 {
                     await ctx.MessageSender.Danger(ctx.Message, "OP", "I don't have the rights to do that!");
                 }
             });
        }

        [Command(Name = "deop", Help = "Disallow a user from using administration commands", Usage = "deop <user>")]
        private async Task DeOP(CommandContext ctx)
        {
            await this.Role(ctx, "DeOP", async user =>
            {
                try
                {
                    IRole role = await ctx.GetOrCreateRole(user, "EnergizeAdmin");
                    if (role != null)
                    {
                        await user.RemoveRoleAsync(role);
                        await ctx.MessageSender.Good(ctx.Message, "DeOP", user.Username + "#" + user.Discriminator + " was succesfully prevented from using administration commands");
                    }
                    else
                    {
                        await ctx.MessageSender.Danger(ctx.Message, "DeOP", "You didn't op anybody yet!");
                    }
                }
                catch
                {
                    await ctx.MessageSender.Danger(ctx.Message, "DeOP", "I don't have the rights to do that!");
                }
            });
        }

        private async Task ClearBase(CommandContext ctx, Action<IMessage, List<IMessage>> callback)
        {
            if(ctx.IsPrivate)
            {
                await ctx.MessageSender.Warning(ctx.Message, "Clear", "You can't do that in DM");
                return;
            }

            if (!ctx.IsAdminUser())
            {
                await ctx.MessageSender.Danger(ctx.Message, "Clear", "You don't have the rights to do that");
                return;
            }

            int amount = 10;
            if (ctx.HasArguments)
            {
                if (int.TryParse(ctx.Arguments[0], out int a))
                {
                    if (a > 100)
                    {
                        a = 100;
                    }

                    if (a < 0)
                    {
                        a = 1;
                    }

                    amount = a;
                }
            }

            IEnumerable<IMessage> messages = ctx.Message.Channel.GetMessagesAsync().Flatten().ToEnumerable();
            List<IMessage> todelete = new List<IMessage>();
            int count = 0;
            foreach (IMessage msg in messages)
            {
                if (count < amount)
                {
                    callback(msg, todelete);
                    count++;
                }
            }

            try
            {
                ITextChannel chan = ctx.Message.Channel as ITextChannel;
                await chan.DeleteMessagesAsync(todelete);
                await ctx.MessageSender.Good(ctx.Message, "Clear", "Cleared " + todelete.Count + " messages among " + amount);
            }
            catch
            {
                await ctx.MessageSender.Danger(ctx.Message, "Clear", "I don't have the rights to do that!");
            }
        }

        [Command(Name = "clear", Help = "Clear the bot messages and commands", Usage = "clear <amounttoremove|nothing>")]
        private async Task Clear(CommandContext ctx)
        {
            ClientMemoryStream stream = ServiceManager.GetService<ClientMemoryStream>("MemoryStream");
            ulong id = (await stream.GetClientInfo()).ID;
            await this.ClearBase(ctx, (msg, todelete) =>
            {
                bool old = (DateTime.Now.Date.Ticks - msg.CreatedAt.Date.Ticks > new DateTime().AddDays(15).Ticks);
                if (!old)
                {
                    SocketMessage mess = msg as SocketMessage;
                    if (msg.Author.Id == id)
                    {
                        todelete.Add(msg);
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(msg.Content))
                        {
                            string cmd = ctx.Handler.GetCmd(msg.Content);
                            if (msg.Content.StartsWith(ctx.Prefix) && ctx.Handler.IsCmdLoaded(cmd))
                            {
                                todelete.Add(msg);
                            }
                        }
                    }
                }
            });
        }

        [Command(Name="clearuser",Help="Clears a specific user's messages",Usage="clearuser <@user|userid>")]
        private async Task ClearUser(CommandContext ctx)
        {
            if(ctx.TryGetUser(ctx.Arguments[0],out SocketUser user,true))
            {
                await this.ClearBase(ctx, (msg, todelete) =>
                {
                    bool old = (DateTime.Now.Date.Ticks - msg.CreatedAt.Date.Ticks > new DateTime().AddDays(15).Ticks);
                    if(msg.Author.Id == user.Id && !old)
                    {
                        todelete.Add(msg);
                    }
                });
            }
            else
            {
                await ctx.MessageSender.Danger(ctx.Message, "Clear", "Couldn't find a user for your input");
            }
        }

        [Command(Name="clearbots",Help="Clear bot messages",Usage="clearbots <amounttoremove|nothing>")]
        private async Task ClearBots(CommandContext ctx)
        {
            await this.ClearBase(ctx, (msg, todelete) =>
            {
                bool old = (DateTime.Now.Date.Ticks - msg.CreatedAt.Date.Ticks > new DateTime().AddDays(15).Ticks);
                if (msg.Author.IsBot && !old)
                {
                    todelete.Add(msg);
                }
            });
        }

        [Command(Name="clearraw",Help="Clear every messages with the amount given",Usage="clearraw <amount>")]
        private async Task ClearRaw(CommandContext ctx)
        {
            await this.ClearBase(ctx, (msg, todelete) =>
            {
                todelete.Add(msg);
            });
        }

        [Command(Name="shame",Help="Adds a message to the \"Hall of Shames\"\nYou can get message ids with discord developer mode",Usage="shame <messageid>")]
        private async Task Shame(CommandContext ctx)
        {
            if(ctx.IsPrivate)
            {
                await ctx.MessageSender.Warning(ctx.Message,"Shame","You can't do that in DM");
                return;
            }

            if (!ctx.IsAdminUser())
            {
                await ctx.MessageSender.Danger(ctx.Message, "Shame", "You don't have the rights to do that");
                return;
            }

            if (!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
                return;
            }

            DBContextPool db = ServiceManager.GetService<DBContextPool>("Database");
            using (DBContext dbctx = await db.GetContext())
            {
                IGuild guild = (ctx.Message.Author as SocketGuildUser).Guild;
                DiscordGuild dbguild = await dbctx.Instance.GetOrCreateGuild(guild.Id);
                ITextChannel c = null;
                if(dbguild.HasHallOfShames)
                {
                    c = await guild.GetChannelAsync(dbguild.HallOfShameID) as ITextChannel;
                }
                else
                {
                    Administration admin = ServiceManager.GetService<Administration>("Administration");
                    try
                    {
                        c = await admin.CreateChannel(ctx, "Hall-Of-Shames",
                            $"Where {ctx.Client.CurrentUser.Username} will post unique messages.");
                        dbguild.HallOfShameID = c.Id;
                        dbguild.HasHallOfShames = true;
                    }
                    catch
                    {
                        await ctx.MessageSender.Danger(ctx.Message, "Shame", "I lack the `Manage Channels` right.");
                    }
                }

                if (ulong.TryParse(ctx.Arguments[0], out ulong mid))
                {
                    IMessage msg = await ctx.Message.Channel.GetMessageAsync(mid);
                    if (msg != null)
                    {
                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithAuthor(msg.Author);
                        builder.WithDescription(msg.Content);
                        builder.WithFooter("#" + msg.Channel.Name);
                        builder.WithTimestamp(msg.CreatedAt);
                        builder.WithColor(ctx.MessageSender.ColorNormal);
                        string url = ctx.Handler.GetImageURLS(msg);
                        if (url != null)
                        {
                            builder.WithImageUrl(url);
                        }

                        IUserMessage posted = await ctx.MessageSender.Send(c as SocketChannel, builder.Build());
                        if (posted != null)
                        {
                            string noreact = string.Empty;
                            try
                            {
                                await posted.AddReactionAsync(new Emoji("🌟"));
                            }
                            catch
                            {
                                noreact = $"\n(I lack `Add reactions` right in \"{c.Name}\"!)";
                            }

                            await ctx.MessageSender.Good(ctx.Message, "Shame", "Message added to hall of shames" + noreact);
                        }
                        else
                        {
                            await ctx.MessageSender.Danger(ctx.Message, "Shame", $"I wasn't able to post in \"{c.Name}\" ?!");
                        }
                    }
                    else
                    {
                        await ctx.MessageSender.Danger(ctx.Message, "Shame", "Sorry this message wasnt found or is not in the same channel");
                    }
                }
                else
                {
                    await ctx.SendBadUsage();
                }
            }
        }

        [Command(Name="delinvs",Help="Deletes messages containing discord invites(toggleable)",Usage="delinvs <nothing>")]
        private async Task DelInvites(CommandContext ctx)
        {
            if (!ctx.IsAdminUser())
            {
                await ctx.MessageSender.Danger(ctx.Message, "DelInvites", "You don't have the rights to do that");
                return;
            }

            if(!ctx.IsPrivate)
            {
                SocketGuildUser user = ctx.Message.Author as SocketGuildUser;
                DBContextPool db = ServiceManager.GetService<DBContextPool>("Database");
                using (DBContext dbctx = await db.GetContext())
                {
                    DiscordGuild dbguild = await dbctx.Instance.GetOrCreateGuild(user.Guild.Id);
                    dbguild.ShouldDeleteInvites = !dbguild.ShouldDeleteInvites;
                    if(dbguild.ShouldDeleteInvites)
                    {
                        await ctx.MessageSender.Good(ctx.Message, "DelInvites", "Invite link messages will now be deleted");
                    }
                    else
                    {
                        await ctx.MessageSender.Good(ctx.Message, "DelInvites", "Invite link messages will no longer be deleted");
                    }
                }
            }
            else
            {
                await ctx.MessageSender.Warning(ctx.Message, "DelInvites", "This is not available in DM");
            }
        }
    }
}
