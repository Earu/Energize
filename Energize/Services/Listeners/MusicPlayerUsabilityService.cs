using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.Services;
using Energize.Interfaces.Services.Listeners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Victoria.Entities;

namespace Energize.Services.Listeners
{
    [Service("MusicUsability")]
    public class MusicPlayerUsabilityService : ServiceImplementationBase, IServiceImplementation
    {
        private static readonly Emoji Emote = new Emoji("⏯");
        private static readonly Regex YTPlaylistPattern = CompiledRegex(@"(?:youtube(?:-nocookie)?\.com\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|\S*?[?&]v=)|youtu\.be\/)([a-zA-Z0-9_-]{11})\W");
        private static readonly Regex YTPattern = CompiledRegex(@"(http(s)?:\/\/)?((w){3}.)?youtu(be|.be)?(\.com)?\/.+");
        private static readonly Regex SCPattern = CompiledRegex(@"https?:\/\/(soundcloud\.com|snd\.sc)\/(.*)");
        private static readonly Regex TwitchPattern = CompiledRegex(@"https?://www.twitch.tv/.+");

        private readonly Logger Logger;
        private readonly ServiceManager ServiceManager;
        private readonly MessageSender MessageSender;

        public MusicPlayerUsabilityService(EnergizeClient client)
        {
            this.Logger = client.Logger;
            this.ServiceManager = client.ServiceManager;
            this.MessageSender = client.MessageSender;
        }

        private static Regex CompiledRegex(string pattern)
            => new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private bool IsYoutubeURL(string url)
            => YTPlaylistPattern.IsMatch(url) || YTPattern.IsMatch(url);

        private bool IsSoundcloudURL(string url)
            => SCPattern.IsMatch(url);

        private bool IsTwitchURL(string url)
            => TwitchPattern.IsMatch(url);

        private bool IsValidURL(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return this.IsYoutubeURL(url) || this.IsSoundcloudURL(url) || this.IsTwitchURL(url);
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
            if (msg.Channel is IDMChannel) return false;
            if (msg.Author.IsBot || msg.Author.IsWebhook) return false;
            if (msg.Embeds.Count < 1 && msg.Attachments.Count < 1) return false;
            CommandHandlingService commands = this.ServiceManager.GetService<CommandHandlingService>("Commands");
            if (commands.IsCommandMessage(msg)) return false;

            return true;
        }

        private string SanitizeYoutubeUrl(string url)
        {
            if (YTPlaylistPattern.IsMatch(url))
            {
                string identifier = YTPlaylistPattern.Match(url).Groups[1].Value;
                return $"https://www.youtube.com/watch?v={identifier}";
            }
            else
            {
                return url;
            }
        }

        private async Task TryPlayUrl(IMusicPlayerService music, ITextChannel textChan, IUserMessage msg, IGuildUser guser, string url)
        {
            SearchResult result = await music.LavaRestClient.SearchTracksAsync(this.SanitizeYoutubeUrl(url));
            List<LavaTrack> tracks = result.Tracks.ToList();
            switch (result.LoadType)
            {
                case LoadType.SearchResult:
                case LoadType.TrackLoaded:
                    if (tracks.Count > 0)
                        await music.AddTrackAsync(guser.VoiceChannel, textChan, tracks[0]);
                    break;
                case LoadType.PlaylistLoaded:
                    if (tracks.Count > 0)
                        await music.AddPlaylistAsync(guser.VoiceChannel, textChan, result.PlaylistInfo.Name, tracks);
                    break;
                case LoadType.LoadFailed:
                case LoadType.NoMatches:
                    await this.MessageSender.Warning(textChan, "music player", $"The content at `{url}` shared by {msg.Author.Mention} could not be played because it is corrupted or could not be found");
                    this.Logger.Nice("music player", ConsoleColor.Yellow, $"Could not play a playable message ({url})");
                    break;
            }
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (!this.IsValidMessage(msg)) return;

            if(msg.Embeds.Any(embed => this.IsValidURL(embed.Url)) || msg.Attachments.Any(attachment => attachment.IsPlayableAttachment()))
            {
                try
                {
                    SocketGuildChannel chan = (SocketGuildChannel)msg.Channel;
                    SocketGuild guild = chan.Guild;
                    if (guild.CurrentUser.GetPermissions(chan).AddReactions)
                    {
                        IUserMessage userMsg = (IUserMessage)msg;
                        await userMsg.AddReactionAsync(Emote);
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.Nice("MusicPlayer", ConsoleColor.Yellow, $"Could not make a playable message usable: {ex.Message}");
                }
            }
        }

        [Event("MessageUpdated")]
        public async Task OnMessageUpdated(Cacheable<IMessage, ulong> _, SocketMessage msg, ISocketMessageChannel __)
        {
            if (!this.IsValidMessage(msg)) return;

            if (msg.Embeds.Any(embed => this.IsValidURL(embed.Url)) || msg.Attachments.Any(attachment => attachment.IsPlayableAttachment()))
            {
                try
                {
                    SocketGuildChannel chan = (SocketGuildChannel)msg.Channel;
                    SocketGuild guild = chan.Guild;
                    if (guild.CurrentUser.GetPermissions(chan).AddReactions)
                    {
                        IUserMessage userMsg = (IUserMessage)msg;
                        if (userMsg.Reactions.ContainsKey(Emote))
                        {
                            ReactionMetadata metadata = userMsg.Reactions[Emote];
                            if (!metadata.IsMe)
                                await userMsg.AddReactionAsync(Emote);
                        }
                        else
                        {
                            await userMsg.AddReactionAsync(Emote);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.Nice("MusicPlayer", ConsoleColor.Yellow, $"Could not make a playable message usable: {ex.Message}");
                }
            }
        }

        [Event("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);

        [Event("ReactionRemoved")]
        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);

        private async Task OnReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!this.IsValidReaction(chan, reaction)) return;
            IGuildUser guser = (IGuildUser)reaction.User.Value;
            if (guser.VoiceChannel == null) return;
            ITextChannel textChan = (ITextChannel)chan;
            IUserMessage msg = await cache.GetOrDownloadAsync();
            if (!this.IsValidMessage(msg)) return;

            IMusicPlayerService music = this.ServiceManager.GetService<IMusicPlayerService>("Music");

            foreach (Embed embed in msg.Embeds)
            {
                if (!this.IsValidURL(embed.Url)) continue;
                await this.TryPlayUrl(music, textChan, msg, guser, embed.Url);
            }

            foreach(Attachment attachment in msg.Attachments)
            {
                if (!attachment.IsPlayableAttachment()) continue;
                await this.TryPlayUrl(music, textChan, msg, guser, attachment.Url);
            }
        }
    }
}
