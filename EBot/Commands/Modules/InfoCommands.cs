using Discord;
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
        [Command(Name = "server", Help = "Gets information about the server", Usage = "server <nothing>")]
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

        [Command(Name = "info", Help = "Gets information relative to the bot", Usage = "info <nothing>")]
        private async Task Info(CommandContext ctx)
        {
            ClientInfo info = await ClientMemoryStream.GetClientInfo();

            string desc = "";
            desc += "**NAME**: " + info.Name + "\n";
            desc += "**PREFIX**: " + info.Prefix + "\n";
            desc += "**COMMANDS**: " + info.CommandAmount + "\n";
            desc += "**SERVERS**: " + info.GuildAmount + "\n";
            desc += "**USERS**: " + info.UserAmount + "\n";
            desc += "**OWNER**: " + info.Owner + "\n";

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithFooter("Info");
            builder.WithThumbnailUrl(info.Avatar);
            builder.WithDescription(desc);
            builder.WithColor(ctx.EmbedReply.ColorGood);
            builder.WithAuthor(ctx.Message.Author);

            await ctx.EmbedReply.Send(ctx.Message, builder.Build());
        }

        [Command(Name = "user", Help = "Gets information about a specific user", Usage = "user <@user|id>")]
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

        [Command(Name = "help", Help = "This command", Usage = "help <command|nothing>")]
        private async Task Help(CommandContext ctx)
        {
            string arg = ctx.Arguments[0];
            if (ctx.HasArguments)
            {
                bool retrieved = ctx.Commands.TryGetValue(arg.ToLower().Trim(), out Command cmd);
                if (retrieved && cmd.Loaded)
                {
                    await ctx.EmbedReply.Good(ctx.Message, "Help [ " + arg + " ]", cmd.GetHelp());
                }
                else
                {
                    await ctx.EmbedReply.Danger(ctx.Message, "Help", "Couldn't find documentation for \"" + arg + "\"");
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

                await ctx.EmbedReply.RespondByDM(ctx.Message, "Help [ all ]", result);
            }
        }

        public void Initialize(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand(this.Server);
            handler.LoadCommand(this.Info);
            handler.LoadCommand(this.User);
            handler.LoadCommand(this.Help);

            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}
