using Discord;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using Energize.Services.Commands;
using Energize.Services.Database;
using Energize.Services.Database.Models;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    [Service("Administration")]
    class Administration
    {
        private EnergizeClient _EClient;

        public Administration(EnergizeClient eclient)
        {
            this._EClient = eclient;
        }

        public async Task<ITextChannel> GetOrCreateChannel(CommandContext ctx, string channame, string topic)
        {
            SocketGuildUser user = ctx.Message.Author as SocketGuildUser;
            SocketGuildChannel chan = user.Guild.Channels.Where(x => x != null && x.Name == channame).FirstOrDefault();
            ITextChannel c = null;
            if (chan == null)
            {
                c = await this.CreateChannel(ctx, channame, topic);
            }
            else
            {
                c = chan as SocketTextChannel;
            }

            return c;
        }

        public async Task<ITextChannel> CreateChannel(CommandContext ctx, string channame, string topic)
        {
            SocketGuildUser user = ctx.Message.Author as SocketGuildUser;
            RestTextChannel created = await user.Guild.CreateTextChannelAsync(channame);
            OverwritePermissions everyoneperms = new OverwritePermissions(
                mentionEveryone: PermValue.Deny,
                sendMessages: PermValue.Deny,
                sendTTSMessages: PermValue.Deny
                );
            OverwritePermissions botperm = new OverwritePermissions(
                sendMessages: PermValue.Allow,
                addReactions: PermValue.Allow
                );
            await created.AddPermissionOverwriteAsync(user.Guild.EveryoneRole, everyoneperms);
            await created.AddPermissionOverwriteAsync(ctx.Client.CurrentUser, botperm);
            await created.ModifyAsync(prop =>
            {
                prop.Topic = topic;
            });

            return created as ITextChannel;
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (msg.Channel is IGuildChannel)
            {
                SocketGuildChannel chan = msg.Channel as SocketGuildChannel;
                string pattern = @"discord\.gg\/.+\s?";
                if (Regex.IsMatch(msg.Content, pattern) && msg.Author.Id != EnergizeConfig.BOT_ID_MAIN)
                {
                    DBContextPool db = ServiceManager.GetService<DBContextPool>("Database");
                    DBContext ctx = await db.GetContext();
                    DiscordGuild dbguild = await ctx.Context.GetOrCreateGuild(chan.Guild.Id);
                    if (dbguild.ShouldDeleteInvites)
                    {
                        CommandHandler chandler = ServiceManager.GetService<CommandHandler>("Commands");
                        try
                        {
                            EmbedBuilder builder = new EmbedBuilder();
                            chandler.MessageSender.BuilderWithAuthor(msg, builder);
                            builder.WithDescription("Your message was removed, it contained an invitation link");
                            builder.WithFooter("Invite Checker");
                            builder.WithColor(chandler.MessageSender.ColorWarning);

                            await msg.DeleteAsync();
                            await chandler.MessageSender.Send(msg, builder.Build());
                        }
                        catch
                        {
                            await chandler.MessageSender.Danger(msg, "Invite Checker", "I couldn't delete this message"
                                + " because I don't have the rights necessary for that");
                        }

                    }
                }
            }
        }

        public async Task InitializeAsync()
        {
            if (File.Exists("restartlog.txt"))
            {
                string content = File.ReadAllText("restartlog.txt");
                ulong id = ulong.Parse(content.Trim());

                await this._EClient.MessageSender.Good(this._EClient.Discord.GetChannel(id), "Restart", "Done restarting.");
                File.Delete("restartlog.txt");
            }
        }
    }
}
