﻿using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.Services;
using Energize.Interfaces.Services.Listeners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using Victoria.Entities;

namespace Energize.Services.Listeners.Music
{
    [Service("MusicUsability")]
    public class MusicPlayerUsabilityService : ServiceImplementationBase, IServiceImplementation
    {
        private static readonly Emoji Emote = new Emoji("⏯");
        private static readonly Regex YTPlaylistRegex = CompiledRegex(@"(?:youtube(?:-nocookie)?\.com\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|\S*?[?&]v=)|youtu\.be\/)([a-zA-Z0-9_-]{11})");
        private static readonly Regex YTRegex = CompiledRegex(@"(https?:\/\/)?(www\.)?(youtube\.com|youtu\.be)\/.+");
        private static readonly Regex SCRegex = CompiledRegex(@"https?:\/\/soundcloud\.com\/[^\/\s]+\/[^\/\s]+");
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

        private bool IsYoutubeURL(string url)
        {
            if (!url.Contains("youtu")) return false;

            return YTPlaylistRegex.IsMatch(url) || YTRegex.IsMatch(url);
        }

        private bool IsSoundcloudURL(string url)
        {
            if (!url.Contains("soundcloud")) return false;

            return SCRegex.IsMatch(url);
        }

        private bool IsTwitchURL(string url)
        {
            if (!url.Contains("twitch")) return false;

            Match match = TwitchRegex.Match(url);
            if (match.Success)
            {
                if (match.Groups[2].Value != "videos")
                    return true;
            }

            return false;
        }

        private bool IsSpotifyURL(string url)
        {
            if (!url.Contains("spotify")) return false;

            return SpotifyRegex.IsMatch(url);
        }

        private bool IsValidURL(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return this.IsYoutubeURL(url) || this.IsSoundcloudURL(url) || this.IsTwitchURL(url) || this.IsSpotifyURL(url);
        }

        private bool IsValidReaction(ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (reaction.Emote?.Name == null) return false;
            if (!reaction.Emote.Name.Equals(Emote.Name)) return false;
            if (reaction.UserId == Config.Instance.Discord.BotID) return false;
            if (chan is IDMChannel || reaction.User.GetValueOrDefault() == null) return false;
            if (reaction.User.Value.IsBot || reaction.User.Value.IsWebhook) return false;

            return true;
        }

        private bool IsValidMessage(IMessage msg)
        {
            if (msg.Author.IsBot || msg.Author.IsWebhook) return false;
            if (msg.Embeds.Count < 1 && msg.Attachments.Count < 1) return false;
            CommandHandlingService commands = this.ServiceManager.GetService<CommandHandlingService>("Commands");
            if (commands.IsCommandMessage(msg)) return false;

            return true;
        }

        private async Task<string> SpotifyToYoutubeURLAsync(string url)
        {
            if (!url.Contains("spotify")) return url;

            Match match = SpotifyRegex.Match(url);
            if (match.Success)
            {
                string spotifyId = match.Groups[1].Value;
                IMusicPlayerService music = this.ServiceManager.GetService<IMusicPlayerService>("Music");
                ITrack track = await music.ConvertSpotifyTrackToYoutubeAsync(spotifyId);
                return track.InnerTrack.Uri.AbsoluteUri;
            }
            else
            {
                return url;
            }
        }

        private string SanitizeYoutubeUrl(string url)
        {
            if (!url.Contains("youtu")) return url;

            Match match = YTPlaylistRegex.Match(url);
            if (match.Success)
            {
                string ytId = match.Groups[1].Value;
                return $"https://www.youtube.com/watch?v={ytId}";
            }
            else
            {
                return url;
            }
        }

        private bool HasPlayableVideo(Embed embed)
        {
            if (embed.Type == EmbedType.Video)
                return embed.Video.HasValue && embed.Video.Value.Url.IsPlayableURL();
            else
                return false;
        }

        private async Task<IUserMessage> SendNonPlayableContent(IGuildUser user, IUserMessage msg, ITextChannel textChan, string url, string error)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder
                .WithAuthorNickname(user)
                .WithColorType(EmbedColorType.Warning)
                .WithFooter("music player")
                .WithDescription("🎶 Could not play/add track:")
                .WithField("URL", $"**{url}**")
                .WithField("Posted By", msg.Author.Mention)
                .WithField("Error", error);

            return await this.MessageSender.Send(textChan, builder.Build());
        }

        private async Task TryPlayUrl(IMusicPlayerService music, ITextChannel textChan, IUserMessage msg, IGuildUser guser, string url)
        {
            url = await this.SpotifyToYoutubeURLAsync(url);
            SearchResult result = await music.LavaRestClient.SearchTracksAsync(this.SanitizeYoutubeUrl(url));
            List<ITrack> tracks = result.Tracks.Select(lavaTrack => (ITrack) new DependentTrack(lavaTrack)).ToList();
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
                    await this.SendNonPlayableContent(guser, msg, textChan, url, "File is corrupted or does not have audio");
                    this.Logger.Nice("music player", ConsoleColor.Yellow, $"Could add/play track from playable content ({url})");
                    break;
                case LoadType.NoMatches:
                    await this.SendNonPlayableContent(guser, msg, textChan, url, "Could not find the track to be added/played");
                    this.Logger.Nice("music player", ConsoleColor.Yellow, $"Could not find match for playable content ({url})");
                    break;
            }
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (!this.IsValidMessage(msg)) return;

            if(msg.Embeds.Any(embed => this.IsValidURL(embed.Url) || this.HasPlayableVideo(embed)) || msg.Attachments.Any(attachment => attachment.IsPlayableAttachment()))
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

            if (msg.Embeds.Any(embed => this.IsValidURL(embed.Url) || this.HasPlayableVideo(embed)) || msg.Attachments.Any(attachment => attachment.IsPlayableAttachment()))
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
                if (this.IsValidURL(embed.Url)) 
                    await this.TryPlayUrl(music, textChan, msg, guser, embed.Url);
                else if(embed.Video.HasValue)
                    await this.TryPlayUrl(music, textChan, msg, guser, embed.Video.Value.Url);
            }

            foreach(Attachment attachment in msg.Attachments)
            {
                if (attachment.IsPlayableAttachment()) 
                    await this.TryPlayUrl(music, textChan, msg, guser, attachment.Url);
            }
        }
    }
}
