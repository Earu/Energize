using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Services.MemoryStream;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Diagnostics;
using Energize.Utils;

namespace Energize.Services.Commands.Modules
{
    [CommandModule("Information")]
    class InfoCommands
    {
        [Command(Name="server",Help="Gets information about the server",Usage="server <nothing>")]
        private async Task Server(CommandContext ctx)
        {
            if (!ctx.IsPrivate)
            {
                SocketGuild guild = (ctx.Message.Channel as IGuildChannel).Guild as SocketGuild;
                RestUser owner = await ctx.RESTClient.GetUserAsync(guild.OwnerId);

                string created = guild.CreatedAt.ToString();
                created = created.Remove(created.Length - 7);
                string region = guild.VoiceRegionId.ToString().ToUpper();

                string info = "";
                info += "**ID:** " + guild.Id + "\n";
                info += "**OWNER:** " + (owner == null ? "NULL\n" : owner.Mention + "\n");
                info += "**MEMBERS:** " + guild.MemberCount + "\n";
                info += "**REGION:** " + region + "\n";
                info += "**CREATED ON:** " + created + "\n";
                info += "**MAIN CHANNEL:** " + guild.DefaultChannel.Name + "\n";

                if (guild.Emotes.Count > 0)
                {
                    info += "\n--- Emotes ---\n";

                    int count = 0;
                    foreach (Emote emoji in guild.Emotes)
                    {
                        info += emoji + " ";
                        count++;
                        if (count >= 10)
                        {
                            info += "\n";
                            count = 0;
                        }
                    }
                }

                await ctx.MessageSender.Send(ctx.Message,guild.Name,info,ctx.MessageSender.ColorGood,guild.IconUrl);
            }
            else
            {
                await ctx.MessageSender.Danger(ctx.Message, "Server", "You can't do that in a DM channel!");
            }
        }

        [Command(Name="info",Help="Gets information relative to the bot",Usage="info <@user|id|nothing>")]
        private async Task Info(CommandContext ctx)
        {
            if(ctx.HasArguments)
            {
                await this.User(ctx);
            }
            else
            {
                ClientInfo info = await (ServiceManager.GetService<ClientMemoryStream>("MemoryStream")).GetClientInfo();
                string invite = "<https://discordapp.com/oauth2/authorize?client_id=" + EnergizeConfig.BOT_ID_MAIN + "&scope=bot&permissions=8>";
                string server = EnergizeConfig.SERVER_INVITE;
                string github = "<https://github.com/Earu/Energize>";

                string desc = "";
                desc += "**NAME**: " + info.Name + "\n";
                desc += "**PREFIX**: " + info.Prefix + "\n";
                desc += "**COMMANDS**: " + info.CommandAmount + "\n";
                desc += "**SERVERS**: " + info.GuildAmount + "\n";
                desc += "**USERS**: " + info.UserAmount + "\n";
                desc += "**OWNER**: " + info.Owner + "\n";

                await ctx.MessageSender.Send(ctx.Message,"Info",desc,ctx.MessageSender.ColorGood,info.Avatar);
                await ctx.MessageSender.SendRaw(ctx.Message,
                    "Official server: " + EnergizeConfig.SERVER_INVITE + "\n"
                    + "Invite link: " + invite + "\n"
                    + "Github: " + github);
            }
        }

        [Command(Name = "invite", Help = "Gets the invite link for the bot", Usage = "invite <nothing>")]
        private async Task Invite(CommandContext ctx)
        {
            string invite = "<https://discordapp.com/oauth2/authorize?client_id=" + EnergizeConfig.BOT_ID_MAIN + "&scope=bot&permissions=8>";

            await ctx.MessageSender.SendRaw(ctx.Message,"Invite link: " + invite);
            await ctx.MessageSender.SendRaw(ctx.Message,"Official server: " + EnergizeConfig.SERVER_INVITE);
        }

        [Command(Name="user",Help="Gets information about a specific user",Usage="user <@user|id>")]
        private async Task User(CommandContext ctx)
        {
            if (ctx.TryGetUser(ctx.Arguments[0], out SocketUser user,true))
            {
                IUser u = null;
                if(user == null)
                {
                    u = await ctx.RESTClient.GetUserAsync(user.Id);
                }
                else
                {
                    u = user;
                }

                List<string> guildnames = new List<string>();
                int maxguilds = 15;
                int leftguilds = 0;
                foreach(SocketGuild guild in ctx.Client.Guilds)
                {
                    if(guild.Users.Any(x => x.Id == u.Id))
                    {
                        if(guildnames.Count < maxguilds)
                        {
                            guildnames.Add(guild.Name);
                        }
                        else
                        {
                            leftguilds++;
                        }
                    }
                }

                string guildinfo = null;
                if(!ctx.IsPrivate)
                {
                    SocketGuildChannel chan = ctx.Message.Channel as SocketGuildChannel;
                    if(chan.Guild.Users.Any(x => x.Id == u.Id))
                    {
                        string guildjoindate = "";
                        string nickname = "";

                        IGuildUser gu = u as IGuildUser;
                        guildjoindate = gu.JoinedAt.ToString();
                        guildjoindate = guildjoindate.Remove(guildjoindate.Length - 7);
                        nickname = gu.Nickname ?? "none";

                        List<string> rolenames = new List<string>();
                        int maxroles = 15;
                        int leftroles = 0;
                        foreach(ulong id in gu.RoleIds)
                        {
                            if(rolenames.Count < maxroles)
                            {
                                IRole role = gu.Guild.GetRole(id);
                                rolenames.Add(role.Name);
                            }
                            else
                            {
                                leftroles++;
                            }
                        }

                        guildinfo = "\n\n--- Guild Related Info ---\n"
                        + "**NICKNAME:** " + nickname + "\n"
                        + "**JOINED GUILD:** " + guildjoindate + "\n"
                        + "**ROLES:** " + string.Join(", ",rolenames) + (leftroles > 0 ? " and " + leftroles + " more..." : "");
                    }
                }

                string created = u.CreatedAt.ToString();
                created = created.Remove(created.Length - 7);

                string desc = "**ID:** " + u.Id + "\n"
                    + "**NAME:** " + u.Username + "#" + u.Discriminator + "\n"
                    + "**BOT:** " + (u.IsBot ? "Yes" : "No") + "\n"
                    + "**STATUS:** " + u.Status + "\n"
                    + "**JOINED DISCORD:** " + created + "\n"
                    + "**SEEN ON:** " + string.Join(", ",guildnames) + (leftguilds > 0 ? " and " + leftguilds + " more..." : "")
                    + (guildinfo == null ? "" : guildinfo);

                await ctx.MessageSender.Send(ctx.Message,"User",desc,ctx.MessageSender.ColorGood,u.GetAvatarUrl(ImageFormat.Auto,512));
            }
            else
            {
                await ctx.MessageSender.Danger(ctx.Message, "User", "Couldn't find any user corresponding to your input");
            }
        }

        [Command(Name="help",Help="This command",Usage="help <command|nothing>")]
        private async Task Help(CommandContext ctx)
        {
            string arg = ctx.Arguments[0];
            if (ctx.HasArguments)
            {
                bool retrieved = ctx.Commands.TryGetValue(arg.ToLower(), out Command cmd);
                if (retrieved && cmd.Loaded)
                {
                    if(cmd.Cmd == ctx.CommandName)
                    {
                        await ctx.MessageSender.SendRaw(ctx.Message,"<:thonkang:387908330800152576>");
                        return;
                    }
                    
                    await ctx.MessageSender.Good(ctx.Message, "Help [ " + arg.ToUpper() + " ]", cmd.GetHelp());
                }
                else
                {
                    if (Command.Modules.ContainsKey(arg))
                    {
                        string result = "";
                        List<Command> cmds = Command.Modules[arg];
                        result += "**COMMANDS:**\n";
                        result += "```";
                        foreach (Command com in cmds)
                        {
                            if (com.Loaded)
                            {
                                result += com.Cmd + ",";
                            }
                        }
                        result = result.Remove(result.Length - 1);
                        result += "```\n\n";

                        await ctx.MessageSender.Good(ctx.Message, "Help [ " + arg.ToUpper() + " ]", result);
                    }
                    else
                    {
                        await ctx.MessageSender.Danger(ctx.Message, "Help", "Couldn't find documentation for \"" + arg + "\"");
                        await ctx.MessageSender.SendRaw(ctx.Message,"Join the official server for more information: " + EnergizeConfig.SERVER_INVITE);
                    }
                }
            }
            else
            {
                if (!ctx.IsPrivate)
                {
                    await ctx.MessageSender.Good(ctx.Message, "Help", "Check your private messages " + ctx.Message.Author.Mention);
                }

                string result = "";
                foreach (KeyValuePair<string, List<Command>> module in Command.Modules)
                {
                    if (Command.IsLoadedModule(module.Key))
                    {
                        List<Command> cmds = module.Value;
                        result += "**" + module.Key.ToUpper() + ":**\n";
                        result += "```";
                        foreach (Command cmd in cmds)
                        {
                            if (cmd.Loaded)
                            {
                                result += cmd.Cmd + ",";
                            }
                        }
                        result = result.Remove(result.Length - 1);
                        result += "```\n\n";
                    }
                }

                await ctx.MessageSender.RespondByDM(ctx.Message, "Help [ ALL ]", result);
            }
        }

        [Command(Name="isadmin",Help="Gets wether or not a user is an admin",Usage="isadmin <@user|id>")]
        private async Task IsAdmin(CommandContext ctx)
        {
            if(!(ctx.Message.Channel is IGuildChannel))
            {
                await ctx.MessageSender.Danger(ctx.Message, "IsAdmin", "You can't do that in a DM");
                return;
            }

            if (ctx.TryGetUser(ctx.Arguments[0],out SocketUser user))
            {
                SocketGuildUser u = user as SocketGuildUser;
                if (u.GuildPermissions.Administrator)
                {
                    await ctx.MessageSender.Good(ctx.Message, "IsAdmin", u.Username + "#" + u.Discriminator + " is an administrator");
                }
                else
                {
                    await ctx.MessageSender.Good(ctx.Message, "IsAdmin", u.Username + "#" + u.Discriminator + " is not an administrator");
                }
            }
            else
            {
                await ctx.MessageSender.Danger(ctx.Message, "IsAdmin", "No user was found with your input");
            }
        }

        [Command(Name="roles",Help="Gets a person roles and relative ids",Usage="roles <@user>")]
        private async Task Roles(CommandContext ctx)
        {
            if(ctx.IsPrivate)
            {
                await ctx.MessageSender.Danger(ctx.Message,"Roles","This is not available in DM");
                return;
            }

            if(ctx.TryGetUser(ctx.Arguments[0],out SocketUser user))
            {
                SocketGuildUser u = user as SocketGuildUser;
                string display = "```";
                foreach(IRole role in u.Roles)
                {
                    display += role.Name + "\t\t" + role.Id + "\n";
                }
                display += "```";

                await ctx.MessageSender.Good(ctx.Message,"Roles",display);
            }
            else
            {
                await ctx.MessageSender.Danger(ctx.Message,"Roles","Couldn't find any user for your input");
            }
        }

        [Command(Name="snipe",Help="Snipes the last message deleted in the channel",Usage="snipe <nothing>")]
        private async Task Snipe(CommandContext ctx)
        {
            
            if(ctx.Cache.LastDeletedMessage == null)
            {
                await ctx.MessageSender.Danger(ctx.Message,ctx.CommandName,"Nothing to snipe");
                return;
            }
            else
            {
                SocketMessage delmsg = ctx.Cache.LastDeletedMessage;
                EmbedBuilder builder = new EmbedBuilder();
                ctx.MessageSender.BuilderWithAuthor(ctx.Message,builder);
                builder.WithColor(ctx.MessageSender.ColorGood);
                string iconurl = delmsg.Author.GetAvatarUrl(ImageFormat.Auto,32);
                builder.WithFooter("Message sniped from " + delmsg.Author.ToString(),iconurl);
                builder.WithTimestamp(delmsg.CreatedAt);
                builder.WithDescription(delmsg.Content);
                string url = ctx.Handler.GetImageURLS(delmsg as IMessage);
                if(url != null)
                {
                    builder.WithImageUrl(url);
                }

                await ctx.MessageSender.Send(ctx.Message,builder.Build());
            }
        }

        [Command(Name="lastchanges",Help="Gets the last changes publicly available",Usage="lastchanges <nothing>")]
        private async Task LastChanges(CommandContext ctx)
        {
            string endpoint = "https://api.github.com/repos/Earu/Energize/commits";
            string json = await HTTP.Fetch(endpoint, ctx.Log);
            GitHub.GitHubCommit[] commits = JSON.Deserialize<GitHub.GitHubCommit[]>(json, ctx.Log);
            if(commits != null && commits.Length > 0)
            {
                string changes = commits[0].commit.message;
                await ctx.MessageSender.Good(ctx.Message, "Last Changes", $"```\n{changes}\n```");
            }
            else
            {
                await ctx.MessageSender.Danger(ctx.Message, "Last Changes", "There was a problem fetching the last changes");
            }
        }

        [Command(Name="playing",Help="Gets the amount of people playing a specific game",Usage="playing <game>")]
        private async Task Playing(CommandContext ctx)
        {
            int count = 0;
            foreach(SocketGuild guild in ctx.Client.Guilds)
            {
                foreach(SocketGuildUser user in guild.Users)
                {
                    if(user.Activity != null)
                    {
                        string activity = user.Activity.Name.ToLower();
                        if (activity == ctx.Input.ToLower())
                        {
                            count++;
                        }
                    }
                }
            }

            if(count > 0)
            {
                await ctx.MessageSender.Good(ctx.Message,"Playing","**" + count + "** users are playing `" + ctx.Input + "`");
            }
            else
            {
                await ctx.MessageSender.Good(ctx.Message,"Playing","Seems like nobody is playing `" + ctx.Input + "`");
            }
        }
    }
}
