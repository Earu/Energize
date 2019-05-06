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

        private readonly Logger _Logger;
        private readonly ServiceManager _ServiceManager;

        public MusicPlayerUsabilityService(EnergizeClient client)
        {
            this._Logger = client.Logger;
            this._ServiceManager = client.ServiceManager;
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

        private bool IsValidMessage(IMessage msg)
        {
            if (msg.Channel is IDMChannel) return false;
            if (msg.Author.IsBot || msg.Author.IsWebhook) return false;
            if (msg.Embeds.Count < 1) return false;
            if (msg.Embeds.Last().Type == EmbedType.Rich) return false;
            CommandHandlingService commands = this._ServiceManager.GetService<CommandHandlingService>("Commands");
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

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (!this.IsValidMessage(msg)) return;

            Embed embed = msg.Embeds.Last();
            if (!this.IsValidURL(embed.Url)) return;

            try
            {
                SocketGuildChannel chan = (SocketGuildChannel)msg.Channel;
                SocketGuild guild = chan.Guild;
                if (guild.CurrentUser.GetPermissions(chan).AddReactions)
                {
                    IUserMessage usermsg = (IUserMessage)msg;
                    await usermsg.AddReactionAsync(Emote);
                }
            }
            catch(Exception ex)
            {
                this._Logger.Nice("MusicPlayer", ConsoleColor.Yellow, $"Could not make a playable link usable: {ex.Message}");
            }
        }

        [Event("MessageUpdated")]
        public async Task OnMessageUpdated(Cacheable<IMessage, ulong> _, SocketMessage msg, ISocketMessageChannel __)
        {
            if (!this.IsValidMessage(msg)) return;

            Embed embed = msg.Embeds.Last();
            if (!this.IsValidURL(embed.Url)) return;

            try
            {
                SocketGuildChannel chan = (SocketGuildChannel)msg.Channel;
                SocketGuild guild = chan.Guild;
                if (guild.CurrentUser.GetPermissions(chan).AddReactions)
                {
                    IUserMessage usermsg = (IUserMessage)msg;
                    if (usermsg.Reactions.ContainsKey(Emote))
                    {
                        ReactionMetadata metadata = usermsg.Reactions[Emote];
                        if (!metadata.IsMe)
                            await usermsg.AddReactionAsync(Emote);
                    }
                    else
                    {
                        await usermsg.AddReactionAsync(Emote);
                    }
                }
            }
            catch (Exception ex)
            {
                this._Logger.Nice("MusicPlayer", ConsoleColor.Yellow, $"Could not make a playable link usable: {ex.Message}");
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
            if (reaction.Emote?.Name == null) return;
            if (!reaction.Emote.Name.Equals(Emote.Name)) return;
            if (reaction.UserId == Config.Instance.Discord.BotID) return;
            if (!(chan is IGuildChannel) || reaction.User.Value == null) return;
            IGuildUser guser = (IGuildUser)reaction.User.Value;
            if (guser.VoiceChannel == null) return;
            ITextChannel textchan = (ITextChannel)chan;

            IUserMessage msg = await cache.GetOrDownloadAsync();
            if (!this.IsValidMessage(msg)) return;
            IEmbed embed = msg.Embeds.Last();
            if (this.IsValidURL(embed.Url))
            {
                IMusicPlayerService music = this._ServiceManager.GetService<IMusicPlayerService>("Music");
                SearchResult result = await music.LavaRestClient.SearchTracksAsync(this.SanitizeYoutubeUrl(embed.Url));
                List<LavaTrack> tracks = result.Tracks.ToList();
                switch(result.LoadType)
                {
                    case LoadType.SearchResult:
                    case LoadType.TrackLoaded:
                        if (tracks.Count > 0)
                            await music.AddTrackAsync(guser.VoiceChannel, textchan, tracks[0]);
                        break;
                    case LoadType.PlaylistLoaded:
                        if (tracks.Count > 0)
                            await music.AddPlaylistAsync(guser.VoiceChannel, textchan, result.PlaylistInfo.Name, tracks);
                        break;
                }
            }
        }
    }
}
