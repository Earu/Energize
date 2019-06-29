using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Services.Listeners.Extendability.ExtendableMessageProviders
{
    internal class DiscordMessageProvider : BaseProvider
    {
        private readonly DiscordShardedClient DiscordClient;

        public DiscordMessageProvider(DiscordShardedClient discordClient, string domain, string pattern) : base(domain, pattern)
        {
            this.DiscordClient = discordClient;
        }

        public override async Task BuildEmbedsAsync(List<Embed> embeds, IUserMessage msg, SocketReaction reaction)
        {
            foreach (Match match in this.Matches(msg.Content))
            {
                ulong guildId = ulong.Parse(match.Groups[1].Value);
                ulong channelId = ulong.Parse(match.Groups[2].Value);
                ulong msgId = ulong.Parse(match.Groups[3].Value);

                SocketGuild guild = this.DiscordClient.GetGuild(guildId);
                SocketTextChannel textChan = guild?.GetTextChannel(channelId);
                if (textChan == null) continue;

                IMessage quotedMsg = await textChan.GetMessageAsync(msgId);
                if (quotedMsg == null) continue;

                EmbedBuilder builder = new EmbedBuilder();
                builder
                    .WithAuthorNickname(quotedMsg)
                    .WithColorType(EmbedColorType.Good)
                    .WithLimitedDescription(string.IsNullOrWhiteSpace(quotedMsg.Content) ? "Empty message." : quotedMsg.Content)
                    .WithTimestamp(quotedMsg.Timestamp)
                    .WithField("Quoted by", $"{reaction.User.Value.Mention} from [**#{quotedMsg.Channel.Name}**]({match.Value})", false);
                embeds.Add(builder.Build());
            }
        }
    }
}
