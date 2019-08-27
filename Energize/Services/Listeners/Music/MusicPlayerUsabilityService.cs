using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Essentials.TrackTypes;
using Energize.Interfaces.Services.Listeners;
using Victoria.Entities;

namespace Energize.Services.Listeners.Music
{
    [Service("MusicUsability")]
    public class MusicPlayerUsabilityService : ServiceImplementationBase
    {
        private static readonly Emoji Emote = new Emoji("⏯");
        private static readonly Regex YtPlaylistRegex = CompiledRegex(@"(?:youtube(?:-nocookie)?\.com\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|\S*?[?&]v=)|youtu\.be\/)([a-zA-Z0-9_-]{11})");
        private static readonly Regex YtRegex = CompiledRegex(@"(https?:\/\/)?(www\.)?(youtube\.com|youtu\.be)\/.+");
        private static readonly Regex ScRegex = CompiledRegex(@"https?:\/\/soundcloud\.com\/[^\/\s]+\/[^\/\s]+");
        private static readonly Regex TwitchRegex = CompiledRegex(@"https?:\/\/(www\.)?twitch\.tv\/([^\/\s]+)");
        private static readonly Regex SpotifyRegex = CompiledRegex(@"https?:\/\/open\.spotify\.com\/track\/([^\/\s]+)");

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

        private static bool IsYoutubeUrl(string url)
            => url.Contains("youtu") && (YtPlaylistRegex.IsMatch(url) || YtRegex.IsMatch(url));

        private static bool IsSoundcloudUrl(string url)
            => url.Contains("soundcloud") && ScRegex.IsMatch(url);

        private static bool IsTwitchUrl(string url)
        {
            if (!url.Contains("twitch")) return false;

            Match match = TwitchRegex.Match(url);
            if (!match.Success) return false;
            
            return match.Groups[2].Value != "videos";
        }

        private static bool IsSpotifyUrl(string url)
            => url.Contains("spotify") && SpotifyRegex.IsMatch(url);

        private static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return IsYoutubeUrl(url) || IsSoundcloudUrl(url) || IsTwitchUrl(url) || IsSpotifyUrl(url);
        }

        private static bool IsValidReaction(ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (reaction.Emote?.Name == null) return false;
            if (!reaction.Emote.Name.Equals(Emote.Name)) return false;
            if (reaction.UserId == Config.Instance.Discord.BotID) return false;
            if (chan is IDMChannel || reaction.User.GetValueOrDefault() == null) return false;
            if (reaction.User.Value.IsBot || reaction.User.Value.IsWebhook) return false;

            return true;
        }

        private bool IsValidMessage(IMessage m, bool checkReactions = true)
        {
            if (!(m is IUserMessage msg)) return false;
            if (msg.Author.IsBot || msg.Author.IsWebhook) return false;
            if (msg.Embeds.Count < 1 && msg.Attachments.Count < 1) return false;
            CommandHandlingService commands = this.ServiceManager.GetService<CommandHandlingService>("Commands");
            if (commands.IsCommandMessage(msg)) return false;
            if (checkReactions && ((msg.Reactions.TryGetValue(Emote, out ReactionMetadata data) && !data.IsMe) || !msg.Reactions.ContainsKey(Emote)))
                return false;

            return true;
        }

        private async Task<SpotifyTrack> SpotifyToTrackAsync(string url)
        {
            if (!url.Contains("spotify")) return null;

            Match match = SpotifyRegex.Match(url);
            if (!match.Success)
                return null;
            
            string spotifyId = match.Groups[1].Value;
            ISpotifyHandlerService spotify = this.ServiceManager.GetService<ISpotifyHandlerService>("Spotify");
            return await spotify.GetTrackAsync(spotifyId);
        }

        private static bool HasPlayableVideo(Embed embed)
        {
            if (embed.Type == EmbedType.Video)
                return embed.Video.HasValue && embed.Video.Value.Url.IsPlayableUrl();
            return false;
        }

        private async Task SendNonPlayableContentAsync(IGuildUser user, IUserMessage msg, ITextChannel textChan, string url, string error)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder
                .WithAuthorNickname(user)
                .WithColorType(EmbedColorType.Warning)
                .WithFooter("music player")
                .WithDescription("🎶 Could not play/add track:")
                .WithField("Url", $"**{url}**")
                .WithField("Posted By", msg.Author.Mention)
                .WithField("Error", error);

            await this.MessageSender.SendAsync(textChan, builder.Build());
        }

        private async Task<bool> TryPlaySpotifyAsync(IMusicPlayerService music, ITextChannel textChan, IGuildUser guser, string url)
        {
            SpotifyTrack track = await this.SpotifyToTrackAsync(url);
            if (track == null) return false;
            
            await music.AddTrackAsync(guser.VoiceChannel, textChan, track);
            return true;
        }

        private async Task TryPlayUrlAsync(IMusicPlayerService music, ITextChannel textChan, IUserMessage msg, IGuildUser guser, string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return; // can be null or empty apparently

            IGuildUser botUser = await guser.Guild.GetCurrentUserAsync();
            await msg.RemoveReactionAsync(Emote, botUser);
            bool played = await this.TryPlaySpotifyAsync(music, textChan, guser, url);
            if (played) return;

            SearchResult result = await music.LavaRestClient.SearchTracksAsync(url);
            List<ILavaTrack> tracks = result.Tracks.ToList();
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
                    await this.SendNonPlayableContentAsync(guser, msg, textChan, url, "File is corrupted or does not have audio");
                    this.Logger.Nice("music player", ConsoleColor.Yellow, $"Could add/play track from playable content ({url})");
                    break;
                case LoadType.NoMatches:
                    await this.SendNonPlayableContentAsync(guser, msg, textChan, url, "Could not find the track to be added/played");
                    this.Logger.Nice("music player", ConsoleColor.Yellow, $"Could not find match for playable content ({url})");
                    break;
                default:
                    await this.SendNonPlayableContentAsync(guser, msg, textChan, url, "Unkown error");
                    this.Logger.Nice("music player", ConsoleColor.Yellow, $"Unknown error ({url})");
                    break;
            }
        }

        [DiscordEvent("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (!this.IsValidMessage(msg, false)) return;

            if(msg.Embeds.Any(embed => IsValidUrl(embed.Url) || HasPlayableVideo(embed)) || msg.Attachments.Any(attachment => attachment.IsPlayableAttachment()))
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

        [DiscordEvent("MessageUpdated")]
        public async Task OnMessageUpdated(Cacheable<IMessage, ulong> _, SocketMessage msg, ISocketMessageChannel __)
        {
            if (!this.IsValidMessage(msg, false)) return;

            if (msg.Embeds.Any(embed => IsValidUrl(embed.Url) || HasPlayableVideo(embed)) || msg.Attachments.Any(attachment => attachment.IsPlayableAttachment()))
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

        [DiscordEvent("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);

        private async Task OnReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!IsValidReaction(chan, reaction)) return;
            IGuildUser guser = (IGuildUser)reaction.User.Value;
            if (guser.VoiceChannel == null) return;
            ITextChannel textChan = (ITextChannel)chan;
            IUserMessage msg = await cache.GetOrDownloadAsync();
            if (!this.IsValidMessage(msg)) return;

            IMusicPlayerService music = this.ServiceManager.GetService<IMusicPlayerService>("Music");

            foreach (IEmbed embed in msg.Embeds)
            {
                if (IsValidUrl(embed.Url)) 
                    await this.TryPlayUrlAsync(music, textChan, msg, guser, embed.Url);
                else if(embed.Video.HasValue)
                    await this.TryPlayUrlAsync(music, textChan, msg, guser, embed.Video.Value.Url);
            }

            foreach(IAttachment attachment in msg.Attachments)
            {
                if (attachment.IsPlayableAttachment()) 
                    await this.TryPlayUrlAsync(music, textChan, msg, guser, attachment.Url);
            }
        }
    }
}
