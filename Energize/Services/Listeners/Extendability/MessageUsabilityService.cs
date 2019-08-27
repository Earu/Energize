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
    public class MessageUsabilityService : ServiceImplementationBase
    {
        private static readonly Emoji EmoteExtend = new Emoji("⏬");

        private readonly MessageSender MessageSender;
        private readonly ServiceManager ServiceManager;

        private readonly Regex InviteRegex;
        private readonly List<BaseProvider> ExtendableMessageProviders;

        public MessageUsabilityService(EnergizeClient client)
        {
            this.MessageSender = client.MessageSender;
            this.ServiceManager = client.ServiceManager;

            this.InviteRegex = new Regex(@"discord(app\.com\/invite|\.gg)\/[A-Za-z0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            this.ExtendableMessageProviders = new List<BaseProvider>
            {
                new DiscordMessageProvider(client.DiscordClient, "discordapp", @"https:\/\/discordapp.com\/channels\/([0-9]+)\/([0-9]+)\/([0-9]+)"),
                new RedditPostProvider(client.Logger, "reddit", @"https?:\/\/www\.reddit\.com\/r\/([A-Za-z0-9]+)\/comments\/.{6}\/"),
                new GitHubRepoProvider(client.Logger, "github", @"https?:\/\/github\.com\/([^\/\s]+)\/([^\/\s]+)"),
                new FaArtworkProvider(client.Logger, "furaffinity", @"https?:\/\/www\.furaffinity\.net\/view\/[0-9]+"),
            };
        }

        private bool HasInviteUrl(IMessage msg)
            => msg.Content.Contains("discord") && this.InviteRegex.IsMatch(msg.Content);

        private bool HasSupportedUrl(IMessage msg)  
            => this.ExtendableMessageProviders.Any(provider => provider.IsMatch(msg.Content));

        private async Task HandleInviteUrlsAsync(SocketMessage msg)
        {
            SocketGuildChannel chan = (SocketGuildChannel)msg.Channel;
            IDatabaseService db = this.ServiceManager.GetService<IDatabaseService>("Database");
            using (IDatabaseContext ctx = await db.GetContextAsync())
            {
                IDiscordGuild dbguild = await ctx.Instance.GetOrCreateGuildAsync(chan.Guild.Id);
                if (!dbguild.ShouldDeleteInvites) return;

                SocketGuildUser botUser = chan.Guild.CurrentUser;
                if (botUser.GetPermissions(chan).ManageMessages)
                {
                    await msg.DeleteAsync();
                    await this.MessageSender.SendWarningAsync(msg, "invite checker", "Your message was removed.");
                }
                else
                {
                    await this.MessageSender.SendWarningAsync(msg, "invite checker", "Found an invite, but could not delete it, missing permission: ManageMessages");
                }
            }
        }

        private static async Task HandleSupportedUrlsAsync(SocketMessage msg)
        {
            SocketGuildChannel chan = (SocketGuildChannel)msg.Channel;
            SocketGuildUser botUser = chan.Guild.CurrentUser;
            if (botUser.GetPermissions(chan).AddReactions)
            {
                IUserMessage userMsg = (IUserMessage)msg;
                await userMsg.AddReactionAsync(EmoteExtend);
            }
        }

        private static bool IsValidReaction(ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (reaction.Emote?.Name == null) return false;
            if (!reaction.Emote.Name.Equals(EmoteExtend.Name)) return false;
            if (reaction.UserId == Config.Instance.Discord.BotID) return false;
            if (!(chan is IGuildChannel) || reaction.User.GetValueOrDefault() == null) return false;
            if (reaction.User.Value.IsBot || reaction.User.Value.IsWebhook) return false;

            return true;
        }

        private bool IsValidMessage(IUserMessage msg)
        {
            if (msg.Author.IsBot || msg.Author.IsWebhook) return false;
            if (!this.HasSupportedUrl(msg)) return false;
            CommandHandlingService commands = this.ServiceManager.GetService<CommandHandlingService>("Commands");
            if (commands.IsCommandMessage(msg)) return false;
            if (!msg.Reactions.ContainsKey(EmoteExtend) || (msg.Reactions.TryGetValue(EmoteExtend, out ReactionMetadata data) && !data.IsMe))
                return false;

            return true;
        }

        [DiscordEvent("MessageReceived")]
        public async Task OnMessageReceivedAsync(SocketMessage msg)
        {
            if (msg.Channel is IDMChannel || msg.Author.Id == Config.Instance.Discord.BotID) return;

            if (this.HasInviteUrl(msg))
                await this.HandleInviteUrlsAsync(msg);
            else if (this.HasSupportedUrl(msg))
                await HandleSupportedUrlsAsync(msg);
        }


        [DiscordEvent("ReactionAdded")]
        public async Task OnReactionAddedAsync(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!IsValidReaction(chan, reaction)) return;
            IUserMessage msg = await cache.GetOrDownloadAsync();
            if (!this.IsValidMessage(msg)) return;
            SocketGuildChannel reactionChan = (SocketGuildChannel)chan;
            SocketGuildUser botUser = reactionChan.Guild.CurrentUser;
            if (!botUser.GetPermissions(reactionChan).AddReactions) return;

            List<Embed> embeds = new List<Embed>();

            foreach(BaseProvider provider in this.ExtendableMessageProviders)
                await provider.BuildEmbedsAsync(embeds, msg, reaction);

            foreach (Embed embed in embeds)
                await this.MessageSender.SendAsync(chan, embed);

            await msg.RemoveReactionAsync(EmoteExtend, botUser);
        }
    }
}
