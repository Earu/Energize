﻿using Discord;
using Discord.Rest;
using Discord.WebSocket;
using EBot.Logs;
using EBot.MemoryStream;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EBot.Commands.Modules
{
    [CommandModule(Name="Info")]
    class InfoCommands : CommandModule,ICommandModule
    {
        [Command(Name="server",Help="Gets information about the server",Usage="server <nothing>")]
        private async Task Server(CommandContext ctx)
        {
            if (!ctx.IsPrivate)
            {
                SocketGuild guild = (ctx.Message.Channel as IGuildChannel).Guild as SocketGuild;
                RestUser owner = await ctx.RESTClient.GetUserAsync(guild.OwnerId);

                string info = "";
                info += "**ID**: " + guild.Id + "\n";
                info += "**OWNER**: " + (owner == null ? "NULL\n" : owner.Username + "#" + owner.Discriminator + "\n");
                info += "**MEMBERS**: " + guild.MemberCount + "\n";
                info += "**REGION**: " + guild.VoiceRegionId + "\n";

                if (guild.Emotes.Count > 0)
                {
                    info += "\n\n**---- EMOJIS ----**\n";

                    int count = 0;
                    foreach (Emote emoji in guild.Emotes)
                    {
                        info += "<:" + emoji.Name + ":" + emoji.Id + ">  ";
                        count++;
                        if (count >= 10)
                        {
                            info += "\n";
                            count = 0;
                        }
                    }
                }

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithThumbnailUrl(guild.IconUrl);
                builder.WithDescription(info);
                builder.WithFooter(guild.Name);
                builder.WithColor(ctx.EmbedReply.ColorGood);
                builder.WithAuthor(ctx.Message.Author);

                await ctx.EmbedReply.Send(ctx.Message, builder.Build());
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Server", "You can't do that in a DM channel!");
            }
        }

        [Command(Name="info",Help="Gets information relative to the bot",Usage="info <nothing>")]
        private async Task Info(CommandContext ctx)
        {
            ClientInfo info = await ClientMemoryStream.GetClientInfo();
            string invite = "https://discordapp.com/oauth2/authorize?client_id=" + EBotConfig.BOT_ID_MAIN + "&scope=bot&permissions=8";
            string server = "https://discord.gg/KJqhQ22";
            string github = "https://github.com/Earu/EBot";

            string desc = "";
            desc += "**NAME**: " + info.Name + "\n";
            desc += "**PREFIX**: " + info.Prefix + "\n";
            desc += "**COMMANDS**: " + info.CommandAmount + "\n";
            desc += "**SERVERS**: " + info.GuildAmount + "\n";
            desc += "**USERS**: " + info.UserAmount + "\n";
            desc += "**OWNER**: " + info.Owner + "\n";
            desc += "\n[**INVITE**](" + invite + ")\t\t[**SERVER**](" + server + ")\t\t[**GITHUB**](" + github + ")";

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithFooter("Info");
            builder.WithThumbnailUrl(info.Avatar);
            builder.WithDescription(desc);
            builder.WithColor(ctx.EmbedReply.ColorGood);
            builder.WithAuthor(ctx.Message.Author);

            await ctx.EmbedReply.Send(ctx.Message, builder.Build());
        }

        [Command(Name = "invite", Help = "Gets the invite link for the bot", Usage = "invite <nothing>")]
        private async Task Invite(CommandContext ctx)
        {
            string invite = "https://discordapp.com/oauth2/authorize?client_id=" + EBotConfig.BOT_ID_MAIN + "&scope=bot&permissions=8";
            string server = "https://discord.gg/KJqhQ22";
            string github = "https://github.com/Earu/EBot";

            await ctx.EmbedReply.Good(ctx.Message, "Invite", "[**INVITE**](" + invite + ")\t\t[**SERVER**](" + server + ")\t\t[**GITHUB**](" + github + ")");
        }

        [Command(Name="user",Help="Gets information about a specific user",Usage="user <@user|id>")]
        private async Task User(CommandContext ctx)
        {
            if (ctx.TryGetUser(ctx.Arguments[0], out SocketUser user))
            {
                RestUser u = await ctx.RESTClient.GetUserAsync(user.Id);
                string created = u.CreatedAt.ToString();
                created = created.Remove(created.Length - 7);
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(ctx.EmbedReply.ColorGood);
                builder.WithThumbnailUrl(u.GetAvatarUrl());
                builder.WithFooter("User");
                builder.WithDescription(
                    "**ID:** " + u.Id + "\n"
                    + "**NAME:** " + u.Username + "#" + u.Discriminator + "\n"
                    + "**BOT:** " + (u.IsBot ? "Yes" : "No") + "\n"
                    + "**STATUS:** " + u.Status + "\n"
                    + "**JOINED DISCORD:** " + created
                );

                await ctx.EmbedReply.Send(ctx.Message, builder.Build());
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "User", "Couldn't find any user corresponding to your input");
            }
        }

        [Command(Name="help",Help="This command",Usage="help <command|nothing>")]
        private async Task Help(CommandContext ctx)
        {
            string arg = ctx.Arguments[0].Trim();
            if (ctx.HasArguments)
            {
                bool retrieved = ctx.Commands.TryGetValue(arg.ToLower(), out Command cmd);
                if (retrieved && cmd.Loaded)
                {
                    await ctx.EmbedReply.Good(ctx.Message, "Help [ " + arg.ToUpper() + " ]", cmd.GetHelp());
                }
                else
                {
                    if (Command.Modules.ContainsKey(arg))
                    {
                        string result = "";
                        List<Command> cmds = Command.Modules[arg];
                        result += "**COMMANDS:**\n";
                        result += "``";
                        foreach (Command com in cmds)
                        {
                            if (com.Loaded)
                            {
                                result += com.Cmd + ",";
                            }
                        }
                        result = result.Remove(result.Length - 1);
                        result += "``\n\n";

                        await ctx.EmbedReply.Good(ctx.Message, "Help [ " + arg.ToUpper() + " ]", result);
                    }
                    else
                    {
                        await ctx.EmbedReply.Danger(ctx.Message, "Help", "Couldn't find documentation for \"" + arg + "\"");
                    }
                }
            }
            else
            {
                if (!ctx.IsPrivate)
                {
                    await ctx.EmbedReply.Good(ctx.Message, "Help", "Check your private messages " + ctx.Message.Author.Mention);
                }

                string result = "";
                foreach (KeyValuePair<string, List<Command>> module in Command.Modules)
                {
                    if (Command.IsLoadedModule(module.Key))
                    {
                        List<Command> cmds = module.Value;
                        result += "**" + module.Key.ToUpper() + ":**\n";
                        result += "``";
                        foreach (Command cmd in cmds)
                        {
                            if (cmd.Loaded)
                            {
                                result += cmd.Cmd + ",";
                            }
                        }
                        result = result.Remove(result.Length - 1);
                        result += "``\n\n";
                    }
                }

                await ctx.EmbedReply.RespondByDM(ctx.Message, "Help [ ALL ]", result);
            }
        }

        [Command(Name="isadmin",Help="Gets wether or not a user is an admin",Usage="isadmin <user>")]
        private async Task IsAdmin(CommandContext ctx)
        {
            if(!(ctx.Message.Channel is IGuildChannel))
            {
                await ctx.EmbedReply.Danger(ctx.Message, "IsAdmin", "You can't do that in a DM");
                return;
            }

            if (ctx.TryGetUser(ctx.Arguments[0],out SocketUser user))
            {
                SocketGuildUser u = user as SocketGuildUser;
                if (u.GuildPermissions.Administrator)
                {
                    await ctx.EmbedReply.Good(ctx.Message, "IsAdmin", u.Username + "#" + u.Discriminator + " is an administrator");
                }
                else
                {
                    await ctx.EmbedReply.Good(ctx.Message, "IsAdmin", u.Username + "#" + u.Discriminator + " is not an administrator");
                }
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "IsAdmin", "No user was found with your input");
            }
        }

        public void Initialize(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand(this.Server);
            handler.LoadCommand(this.Info);
            handler.LoadCommand(this.User);
            handler.LoadCommand(this.Help);
            handler.LoadCommand(this.IsAdmin);

            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}