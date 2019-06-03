using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.DatabaseModels;
using Energize.Interfaces.Services.Database;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    class RedditPost
    {
        [JsonProperty("data")]
        public RedditPostData Data { get; set; }
    }

    class RedditPostData
    {
        [JsonProperty("children")]
        public RedditInnerPostData[] Children { get; set; }
    }

    class RedditInnerPostData
    {
        [JsonProperty("data")]
        public RedditInnerPost Data { get; set; }
    }

    class RedditInnerPost
    {
        [JsonProperty("post_hint")]
        public string Type { get; set; }

        [JsonProperty("subreddit")]
        public string SubReddit { get; set; }

        [JsonProperty("selftext")]
        public string Content { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("upvote_ratio")]
        public double UpvoteRatio { get; set; }

        [JsonProperty("locked")]
        public bool Locked { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("num_crossposts")]
        public long CrossPostCount { get; set; }

        [JsonProperty("num_comments")]
        public long CommentCount { get; set; }

        [JsonProperty("permalink")]
        public string PermaLink { get; set; }

        [JsonProperty("url")]
        public string URL { get; set; }

        [JsonProperty("subreddit_subscribers", NullValueHandling = NullValueHandling.Ignore)]
        public long SubredditSubscriberCount { get; set; }

        [JsonProperty("created_utc")]
        public double Created { get; set; }

        [JsonProperty("is_video")]
        public bool IsVideo { get; set; }
    }

    [Service("MessageUsability")]
    class MessageUsabilityService : ServiceImplementationBase
    {
        private static readonly Emoji Emote = new Emoji("⏬");

        private readonly DiscordShardedClient DiscordClient;
        private readonly MessageSender MessageSender;
        private readonly ServiceManager ServiceManager;
        private readonly Logger Logger;

        private readonly Regex InviteRegex;
        private readonly Dictionary<string, Regex> URLRegexes;

        public MessageUsabilityService(EnergizeClient client)
        {
            this.DiscordClient = client.DiscordClient;
            this.MessageSender = client.MessageSender;
            this.ServiceManager = client.ServiceManager;
            this.Logger = client.Logger;

            this.InviteRegex = this.CompiledRegex(@"discord\.gg\/.+\s?");
            this.URLRegexes = new Dictionary<string, Regex>
            {
                ["DiscordMsg"] = this.CompiledRegex(@"https:\/\/discordapp.com\/channels\/([0-9]+)\/([0-9]+)\/([0-9]+)"),
                ["Reddit"] = this.CompiledRegex(@"https?:\/\/www\.reddit\.com\/r\/([A-Za-z0-9]+)\/comments\/.{6}\/"),
            };
        }

        private Regex CompiledRegex(string pattern)
            => new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private bool HasInviteURL(IMessage msg)
            => this.InviteRegex.IsMatch(msg.Content);

        private bool HasSupportedURL(IMessage msg)  
            => this.URLRegexes.Any(pattern => pattern.Value.IsMatch(msg.Content));

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

        private async Task HandleURLsAsync(SocketMessage msg)
        {
            SocketGuildChannel chan = (SocketGuildChannel)msg.Channel;
            SocketGuildUser botUser = chan.Guild.CurrentUser;
            if (botUser.GetPermissions(chan).AddReactions)
            {
                IUserMessage userMsg = (IUserMessage)msg;
                await userMsg.AddReactionAsync(Emote);
            }
        }

        private async Task BuildDiscordMessageEmbedsAsync(List<Embed> embeds, IUserMessage msg, SocketReaction reaction)
        {
            Regex msgLinkRegex = this.URLRegexes["DiscordMsg"];
            foreach (Match match in msgLinkRegex.Matches(msg.Content))
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
                    .WithLimitedDescription(string.IsNullOrWhiteSpace(quotedMsg.Content) ? "Empty message." : quotedMsg.Content)
                    .WithTimestamp(quotedMsg.Timestamp)
                    .WithField("Quoted by", $"{reaction.User.Value.Mention} from [**#{quotedMsg.Channel.Name}**]({match.Value})", false);
                embeds.Add(builder.Build());
            }
        }

        private async Task BuildRedditEmbedsAsync(List<Embed> embeds, IUserMessage msg)
        {
            Regex redditRegex = this.URLRegexes["Reddit"];
            foreach(Match match in redditRegex.Matches(msg.Content))
            {
                string json = await HttpClient.GetAsync($"{match.Value}.json", this.Logger);
                RedditPost[] posts = JsonPayload.Deserialize<RedditPost[]>(json, this.Logger);

                RedditPost post = posts.FirstOrDefault();
                if (post == null) continue;

                RedditInnerPost innerPost = post.Data.Children.FirstOrDefault()?.Data;
                if (innerPost == null) continue;

                string content = innerPost.Content;
                EmbedBuilder builder = new EmbedBuilder();
                builder
                    .WithAuthorNickname(msg)
                    .WithColor(this.MessageSender.ColorGood)
                    .WithField("SubReddit", $"r/{innerPost.SubReddit}/", false)
                    .WithField("Upvote Percentage", $"{innerPost.UpvoteRatio * 100}%")
                    .WithField("Comments", innerPost.CommentCount)
                    .WithField("Cross-posts", innerPost.CrossPostCount)
                    .WithField("Locked", innerPost.Locked)
                    .WithField("SubReddit Subscribers", innerPost.SubredditSubscriberCount)
                    .WithUrl($"https://www.reddit.com/{innerPost.PermaLink}")
                    .WithTitle(innerPost.Title);

                if (innerPost.IsVideo)
                    builder.WithDescription($"Video post, [**open in your browser**]({innerPost.URL}/DASH_720?source=fallback) to see it.");
                else
                    switch (innerPost.Type)
                    {
                        case "image":
                            builder.WithImageUrl(innerPost.URL);
                            break;
                        default:
                            builder.WithLimitedDescription(string.IsNullOrWhiteSpace(content) ? "Empty post." : content);
                            break;
                    }
                 
                embeds.Add(builder.Build());
            }
        }

        private bool IsValidReaction(ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (reaction.Emote?.Name == null) return false;
            if (!reaction.Emote.Name.Equals(Emote.Name)) return false;
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
            if (!(msg.Reactions.ContainsKey(Emote) && msg.Reactions[Emote].IsMe)) return false;

            return true;
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (msg.Channel is IDMChannel || msg.Author.Id == Config.Instance.Discord.BotID) return;

            if (this.HasInviteURL(msg))
                await this.HandleInviteURLsAsync(msg);
            else if (this.HasSupportedURL(msg))
                await this.HandleURLsAsync(msg);
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
            await this.BuildDiscordMessageEmbedsAsync(embeds, msg, reaction);
            await this.BuildRedditEmbedsAsync(embeds, msg);

            foreach (Embed embed in embeds)
                await this.MessageSender.Send(chan, embed);

            await msg.RemoveReactionAsync(Emote, botUser);
        }
    }
}
