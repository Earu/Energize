using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.DatabaseModels;
using Energize.Interfaces.Services.Database;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    [Service("MessageUsability")]
    class MessageUsabilityService : ServiceImplementationBase
    {
        private static readonly Emoji Emote = new Emoji("⏬");

        private readonly DiscordShardedClient DiscordClient;
        private readonly MessageSender MessageSender;
        private readonly ServiceManager ServiceManager;
        private readonly Regex InvitePattern;
        private readonly Regex MessageLinkPattern;

        public MessageUsabilityService(EnergizeClient client)
        {
            this.DiscordClient = client.DiscordClient;
            this.MessageSender = client.MessageSender;
            this.ServiceManager = client.ServiceManager;
            this.InvitePattern = new Regex(@"discord\.gg\/.+\s?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            this.MessageLinkPattern = new Regex(@"https:\/\/discordapp.com\/channels\/([0-9]+)\/([0-9]+)\/([0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        private bool HasInvite(IMessage msg)
            => this.InvitePattern.IsMatch(msg.Content);

        private bool HasMessageLink(IMessage msg)
            => this.MessageLinkPattern.IsMatch(msg.Content);

        private async Task HandleInviteMessage(SocketMessage msg)
        {
            SocketGuildChannel chan = (SocketGuildChannel)msg.Channel;
            IDatabaseService db = this.ServiceManager.GetService<IDatabaseService>("Database");
            using (IDatabaseContext ctx = await db.GetContext())
            {
                IDiscordGuild dbguild = await ctx.Instance.GetOrCreateGuild(chan.Guild.Id);
                if (!dbguild.ShouldDeleteInvites) return;

                SocketGuildUser botUser = chan.Guild.CurrentUser;
                if (botUser.GetPermissions(chan).ManageMessages)
                {
                    await msg.DeleteAsync();
                    await this.MessageSender.Warning(msg, "invite checker", "Your message was removed.");
                }
                else
                {
                    await this.MessageSender.Warning(msg, "invite checker", "Found an invite, but could not delete it, missing permission: ManageMessages");
                }
            }
        }

        private async Task HandleMessageLink(SocketMessage msg)
        {
            SocketGuildChannel chan = (SocketGuildChannel)msg.Channel;
            SocketGuildUser botUser = chan.Guild.CurrentUser;
            if (botUser.GetPermissions(chan).AddReactions)
            {
                IUserMessage userMsg = (IUserMessage)msg;
                await userMsg.AddReactionAsync(Emote);
            }
        }

        private bool IsValidReaction(ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (reaction.Emote?.Name == null) return false;
            if (!reaction.Emote.Name.Equals(Emote.Name)) return false;
            if (reaction.UserId == Config.Instance.Discord.BotID) return false;
            if (!(chan is IGuildChannel) || reaction.User.Value == null) return false;

            return true;
        }

        private bool IsValidMessage(IMessage msg)
        {
            if (msg.Author.IsBot || msg.Author.IsWebhook) return false;
            if (!this.HasMessageLink(msg)) return false;
            CommandHandlingService commands = this.ServiceManager.GetService<CommandHandlingService>("Commands");
            if (commands.IsCommandMessage(msg)) return false;

            return true;
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (msg.Channel is IDMChannel || msg.Author.Id == Config.Instance.Discord.BotID) return;

            if (this.HasInvite(msg))
                await this.HandleInviteMessage(msg);
            else if (this.HasMessageLink(msg))
                await this.HandleMessageLink(msg);
        }


        [Event("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!this.IsValidReaction(chan, reaction)) return;
            IUserMessage msg = await cache.GetOrDownloadAsync();
            if (!this.IsValidMessage(msg)) return;
            SocketGuildChannel reactionChan = (SocketGuildChannel)chan;
            SocketGuildUser botUser = reactionChan.Guild.CurrentUser;
            if (!botUser.GetPermissions(reactionChan).AddReactions) return;

            foreach (Match match in this.MessageLinkPattern.Matches(msg.Content))
            {
                ulong guildId = ulong.Parse(match.Groups[1].Value);
                ulong channelId = ulong.Parse(match.Groups[2].Value);
                ulong msgId = ulong.Parse(match.Groups[3].Value);

                SocketGuild guild = this.DiscordClient.GetGuild(guildId);
                if (guild == null) continue;

                SocketTextChannel textChan = guild.GetTextChannel(channelId);
                if (textChan == null) continue;

                IMessage quotedMsg = await textChan.GetMessageAsync(msgId);
                if (quotedMsg == null) continue;

                EmbedBuilder builder = new EmbedBuilder();
                builder
                    .WithAuthorNickname(quotedMsg)
                    .WithColor(this.MessageSender.ColorGood)
                    .WithDescription(string.IsNullOrWhiteSpace(quotedMsg.Content) ? "empty message." : quotedMsg.Content)
                    .WithTimestamp(quotedMsg.Timestamp)
                    .WithField("Quoted by", $"{reaction.User.Value.Mention} from [**#{quotedMsg.Channel.Name}**]({match.Value})", false);
                await this.MessageSender.Send(chan, builder.Build());
            }

            if (botUser.GetPermissions(reactionChan).ManageMessages)
                await msg.DeleteAsync();
            else
                await msg.RemoveReactionAsync(Emote, botUser);
        }
    }
}
