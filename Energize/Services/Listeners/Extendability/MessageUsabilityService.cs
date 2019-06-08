using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.DatabaseModels;
using Energize.Interfaces.Services.Database;
using Energize.Services.Listeners.Extendability.ExtendableMessageProviders;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Services.Listeners.Extendability
{
    [Service("MessageUsability")]
    class MessageUsabilityService : ServiceImplementationBase
    {
        private static readonly Emoji EmoteExtend = new Emoji("⏬");

        private readonly DiscordShardedClient DiscordClient;
        private readonly MessageSender MessageSender;
        private readonly ServiceManager ServiceManager;
        private readonly Logger Logger;

        private readonly Regex InviteRegex;
        private readonly List<BaseProvider> ExtendableMessageProviders;

        public MessageUsabilityService(EnergizeClient client)
        {
            this.DiscordClient = client.DiscordClient;
            this.MessageSender = client.MessageSender;
            this.ServiceManager = client.ServiceManager;
            this.Logger = client.Logger;

            this.InviteRegex = new Regex(@"discord\.gg\/.+\s?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            this.ExtendableMessageProviders = new List<BaseProvider>
            {
                new DiscordMessageProvider(this.DiscordClient, @"https:\/\/discordapp.com\/channels\/([0-9]+)\/([0-9]+)\/([0-9]+)"),
                new RedditPostProvider(this.Logger, @"https?:\/\/www\.reddit\.com\/r\/([A-Za-z0-9]+)\/comments\/.{6}\/"),
                new GitHubRepoProvider(this.Logger, @"https?:\/\/github\.com\/([^\/\s]+)\/([^\/\s]+)"),
                new FAArtworkProvider(this.Logger, @"https?:\/\/www\.furaffinity\.net\/view\/[0-9]+"),
            };
        }

        private bool HasInviteURL(IMessage msg)
            => this.InviteRegex.IsMatch(msg.Content);

        private bool HasSupportedURL(IMessage msg)  
            => this.ExtendableMessageProviders.Any(provider => provider.IsMatch(msg.Content));

        private async Task HandleInviteURLsAsync(SocketMessage msg)
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

        private async Task HandleSupportedURLsAsync(SocketMessage msg)
        {
            SocketGuildChannel chan = (SocketGuildChannel)msg.Channel;
            SocketGuildUser botUser = chan.Guild.CurrentUser;
            if (botUser.GetPermissions(chan).AddReactions)
            {
                IUserMessage userMsg = (IUserMessage)msg;
                await userMsg.AddReactionAsync(EmoteExtend);
            }
        }

        private bool IsValidReaction(ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (reaction.Emote?.Name == null) return false;
            if (!reaction.Emote.Name.Equals(EmoteExtend.Name)) return false;
            if (reaction.UserId == Config.Instance.Discord.BotID) return false;
            if (!(chan is IGuildChannel) || reaction.User.Value == null) return false;
            if (reaction.User.Value.IsBot || reaction.User.Value.IsWebhook) return false;

            return true;
        }

        private bool IsValidMessage(IUserMessage msg)
        {
            if (msg.Author.IsBot || msg.Author.IsWebhook) return false;
            if (!this.HasSupportedURL(msg)) return false;
            CommandHandlingService commands = this.ServiceManager.GetService<CommandHandlingService>("Commands");
            if (commands.IsCommandMessage(msg)) return false;
            if (!msg.Reactions.ContainsKey(EmoteExtend) || (msg.Reactions.ContainsKey(EmoteExtend) && !msg.Reactions[EmoteExtend].IsMe)) return false;

            return true;
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (msg.Channel is IDMChannel || msg.Author.Id == Config.Instance.Discord.BotID) return;

            if (this.HasInviteURL(msg))
                await this.HandleInviteURLsAsync(msg);
            else if (this.HasSupportedURL(msg))
                await this.HandleSupportedURLsAsync(msg);
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

            List<Embed> embeds = new List<Embed>();

            foreach(BaseProvider provider in this.ExtendableMessageProviders)
                await provider.BuildEmbedsAsync(embeds, msg, reaction);

            foreach (Embed embed in embeds)
                await this.MessageSender.Send(chan, embed);

            await msg.RemoveReactionAsync(EmoteExtend, botUser);
        }
    }
}
