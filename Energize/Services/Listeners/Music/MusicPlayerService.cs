using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Essentials.MessageConstructs;
using Energize.Essentials.TrackTypes;
using Energize.Interfaces.Services.Database;
using Energize.Interfaces.Services.Listeners;
using Energize.Interfaces.Services.Senders;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;

namespace Energize.Services.Listeners.Music
{
    [Service("Music")]
    public class MusicPlayerService : ServiceImplementationBase, IMusicPlayerService
    {
        private readonly DiscordShardedClient Client;
        private readonly LavaShardClient LavaClient;
        private readonly Logger Logger;
        private readonly MessageSender MessageSender;
        private readonly ServiceManager ServiceManager;
        private readonly ConcurrentDictionary<ulong, IEnergizePlayer> Players;
        private readonly SpotifyWebAPI Spotify;
        private readonly Timer SpotifyAuthTimer;
        private readonly Random Rand;

        public MusicPlayerService(EnergizeClient client)
        {
            Players = new ConcurrentDictionary<ulong, IEnergizePlayer>();

            Client = client.DiscordClient;
            Logger = client.Logger;
            MessageSender = client.MessageSender;
            ServiceManager = client.ServiceManager;
            LavaClient = new LavaShardClient();
            Spotify = new SpotifyWebAPI
            {
                TokenType = "Bearer",
                UseAuth = true,
                UseAutoRetry = true
            };

            SpotifyAuthTimer = new Timer(async _ =>
            {
                string json = await HttpClient.PostAsync("https://accounts.spotify.com/api/token?grant_type=client_credentials", string.Empty, Logger, null, req => 
                {
                    byte[] credBytes = Encoding.UTF8.GetBytes($"{Config.Instance.Spotify.ClientID}:{Config.Instance.Spotify.ClientSecret}");
                    req.Headers[HttpRequestHeader.Authorization] = $"Basic {Convert.ToBase64String(credBytes)}";
                    req.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                });

                var keys = JsonPayload.Deserialize<Dictionary<string, string>>(json, Logger);
                if (keys.ContainsKey("access_token"))
                    Spotify.AccessToken = keys["access_token"];
            });

            Rand = new Random();

            LavaClient.OnTrackException += this.OnTrackIssue;
            LavaClient.OnTrackStuck += async (ply, lavaTrack, _) => await this.OnTrackIssue(ply, lavaTrack);
            LavaClient.OnTrackFinished += this.OnTrackFinished;
            LavaClient.Log += async logMsg => Logger.Nice("Lavalink", ConsoleColor.Magenta, logMsg.Message);
            LavaClient.OnPlayerUpdated += this.OnPlayerUpdated;
        }

        public override Task InitializeAsync()
        {
            SpotifyAuthTimer.Change(0, 3600 * 1000);
            return Task.CompletedTask;
        }

        private async Task OnPlayerUpdated(LavaPlayer lply, LavaTrack lavaTrack, TimeSpan position)
        {
            if (Players.TryGetValue(lply.VoiceChannel.GuildId, out IEnergizePlayer ply))
            {
                if (ply.TrackPlayer != null)
                {
                    if (!lavaTrack.IsStream && !ply.IsPaused)
                        await ply.TrackPlayer.Update(lavaTrack, ply.Volume, ply.IsPaused, ply.IsLooping, true);

                    ply.Refresh();
                }
            }

            IGuild guild = lply.VoiceChannel.Guild;
            string msg = $"Updated track <{lavaTrack.Title}> ({position}) for player in guild <{guild.Name}>";
            Logger.LogTo("victoria.log", msg);
        }

        public LavaRestClient LavaRestClient { get; private set; }

        private bool SanitizeCheck(IVoiceChannel vc, ITextChannel chan)
            => vc != null && chan != null;

        public async Task<IEnergizePlayer> ConnectAsync(IVoiceChannel vc, ITextChannel chan)
        {
            if (!SanitizeCheck(vc, chan)) return null;

            try
            {
                IEnergizePlayer ply;
                if (Players.ContainsKey(vc.GuildId))
                {
                    ply = Players[vc.GuildId];
                    if (ply.Lavalink == null) // in case we lose the player object
                        ply.Lavalink = await LavaClient.ConnectAsync(vc, chan);
                }
                else
                {
                    ply = new EnergizePlayer(await LavaClient.ConnectAsync(vc, chan));
                    Logger.Nice("MusicPlayer", ConsoleColor.Magenta, $"Connected to VC in guild <{vc.Guild}>");
                    if (Players.TryAdd(vc.GuildId, ply))
                    {
                        ply.BecameInactive += async () =>
                        {
                            Logger.Nice("MusicPlayer", ConsoleColor.Yellow, $"Connected player became inactive in guild <{ply.VoiceChannel.Guild}>");
                            await DisconnectAsync(vc);
                        };
                    }
                }

                LavaClient.UpdateTextChannel(vc.GuildId, chan);
                if (vc.Id != ply.Lavalink.VoiceChannel.Id)
                    await LavaClient.MoveChannelsAsync(vc);

                return ply;
            }
            catch(Exception ex)
            {
                if (ex is ObjectDisposedException)
                    Logger.Nice("MusicPlayer", ConsoleColor.Red, "Could not connect, threading issue from Discord.NET");
                else
                    Logger.Danger(ex);

                await DisconnectAsync(vc);

                return null;
            }
        }

        public async Task DisconnectAsync(IVoiceChannel vc)
        {
            if (vc == null) return;

            try
            {
                await LavaClient.DisconnectAsync(vc);
                if (Players.TryRemove(vc.GuildId, out IEnergizePlayer ply))
                {
                    Logger.Nice("MusicPlayer", ConsoleColor.Magenta, $"Disconnected from VC in guild <{vc.Guild}>");
                    ply.Disconnected = true;
                    if (ply.TrackPlayer != null)
                        await ply.TrackPlayer.DeleteMessage();
                }
            }
            catch (Exception ex)
            {
                if (Players.TryRemove(vc.GuildId, out IEnergizePlayer ply))
                {
                    ply.Disconnected = true;
                    if (ply.TrackPlayer != null)
                        await ply.TrackPlayer.DeleteMessage();
                }
                
                if (ex is ObjectDisposedException)
                    Logger.Nice("MusicPlayer", ConsoleColor.Red, "Could not disconnect, threading issue from Discord.NET");
                else
                    Logger.Danger(ex);
            }
        }

        public async Task DisconnectAllPlayersAsync()
        {
            foreach (KeyValuePair<ulong, IEnergizePlayer> ply in Players)
                await DisconnectAsync(ply.Value.VoiceChannel);
        }

        public async Task<IUserMessage> AddTrackAsync(IVoiceChannel vc, ITextChannel chan, LavaTrack lavaTrack)
        {
            IEnergizePlayer ply = await ConnectAsync(vc, chan);
            if (ply == null) return null;

            if (ply.IsPlaying)
            {
                ply.Queue.Enqueue(lavaTrack);
                return await SendNewTrackAsync(chan, lavaTrack);
            }
            await ply.Lavalink.PlayAsync(lavaTrack);
            return await SendPlayerAsync(ply, lavaTrack, chan);
        }

        public async Task<IUserMessage> AddTrackAsync(IVoiceChannel vc, ITextChannel chan, IAsyncLazyLoadTrack lavaTrack)
        {
            IEnergizePlayer ply = await ConnectAsync(vc, chan);
            if (ply == null) return null;

            if (ply.IsPlaying)
            {
                ply.Queue.Enqueue(await lavaTrack.GetInnerTrackAsync());
                return await SendNewTrackAsync(chan, await lavaTrack.ToLavaTrackAsync());
            }
            await ply.Lavalink.PlayAsync(await lavaTrack.GetInnerTrackAsync());
            return await SendPlayerAsync(ply, await lavaTrack.ToLavaTrackAsync(), chan);
        }

        public async Task<IUserMessage> PlayRadioAsync(IVoiceChannel vc, ITextChannel chan, LavaTrack lavaTrack)
        {
            IEnergizePlayer ply = await ConnectAsync(vc, chan);
            if (ply == null) return null;

            RadioTrack radio = new RadioTrack(lavaTrack);
            ply.Queue.Clear();
            ply.CurrentRadioLava = radio;
            await ply.Lavalink.PlayAsync(lavaTrack);
            return await SendPlayerAsync(ply, radio, chan);
        }

        public async Task<List<IUserMessage>> AddPlaylistAsync(IVoiceChannel vc, ITextChannel chan, string name, IEnumerable<LavaTrack> trs)
        {
            IEnergizePlayer ply = await ConnectAsync(vc, chan);
            if (ply == null) return null;

            List<LavaTrack> tracks = trs.ToList();
            if (tracks.Count < 1)
                return new List<IUserMessage>
                {
                    await MessageSender.Warning(chan, "music player", "The loaded playlist does not contain any tracks")
                };

            if (ply.IsPlaying)
            {
                foreach (LavaTrack track in tracks)
                    ply.Queue.Enqueue(track);

                return new List<IUserMessage>
                {
                    await MessageSender.Good(chan, "music player", $"🎶 Added `{tracks.Count}` tracks from `{name}`")
                };
            }
            LavaTrack lavaTrack = tracks[0];
            tracks.RemoveAt(0);

            if (tracks.Count > 0)
                foreach (LavaTrack tr in tracks)
                    ply.Queue.Enqueue(tr);

            await ply.Lavalink.PlayAsync(lavaTrack);
            return new List<IUserMessage>
            {
                await MessageSender.Good(chan, "music player", $"🎶 Added `{tracks.Count}` tracks from `{name}`"),
                await SendPlayerAsync(ply, lavaTrack, chan)
            };
        }

        public async Task<bool> LoopTrackAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await ConnectAsync(vc, chan);
            if (ply == null) return false;

            bool isLooping = ply.IsLooping;
            ply.IsLooping = !isLooping;
            return !isLooping;
        }

        public async Task<bool> AutoplayTrackAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await ConnectAsync(vc, chan);
            if (ply == null) return false;

            bool autoplay = ply.Autoplay;
            ply.Autoplay = !autoplay;

            return !autoplay;
        }

        public async Task ShuffleTracksAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await ConnectAsync(vc, chan);
            if (ply == null) return;

            ply.Queue.Shuffle();
        }

        public async Task ClearTracksAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await ConnectAsync(vc, chan);
            if (ply == null) return;

            ply.Queue.Clear();
        }

        public async Task PauseTrackAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await ConnectAsync(vc, chan);
            if (ply == null) return;

            if (ply.IsPlaying && !ply.IsPaused)
                await ply.Lavalink.PauseAsync();
        }

        public async Task ResumeTrackAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await ConnectAsync(vc, chan);
            if (ply == null) return;

            if (ply.IsPlaying && ply.IsPaused)
                await ply.Lavalink.ResumeAsync();
        }

        public async Task SkipTrackAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await ConnectAsync(vc, chan);
            if (ply == null) return;

            if (ply.CurrentRadioLava != null)
                ply.CurrentRadioLava = null;

            if (ply.IsPlaying)
                await ply.Lavalink.StopAsync();
        }

        public async Task SetTrackVolumeAsync(IVoiceChannel vc, ITextChannel chan, int vol)
        {
            vol = Math.Clamp(vol, 0, 200);
            IEnergizePlayer ply = await ConnectAsync(vc, chan);
            if (ply == null) return;

            if (ply.IsPlaying)
                await ply.Lavalink.SetVolumeAsync(vol);
        }

        public async Task SeekTrackAsync(IVoiceChannel vc, ITextChannel chan, int amount)
        {
            IEnergizePlayer ply = await ConnectAsync(vc, chan);
            if (ply == null) return;
            if (!ply.IsPlaying) return;

            LavaTrack lavaTrack = ply.CurrentTrack;
            TimeSpan total = lavaTrack.Position.Add(TimeSpan.FromSeconds(amount));
            if (total < lavaTrack.Length && total >= TimeSpan.Zero)
                await ply.Lavalink.SeekAsync(total);
        }

        public ServerStats LavalinkStats { get => LavaClient.ServerStats; }

        public int PlayerCount { get => Players.Count; }

        private async Task<string> GetThumbnailAsync(LavaTrack track)
        {
            try
            {
                return await track.FetchThumbnailAsync();
            }
            catch
            {
                return string.Empty;
            }
        }
        
        public async Task<IUserMessage> SendQueueAsync(IVoiceChannel vc, IMessage msg)
        {
            IEnergizePlayer ply = await ConnectAsync(vc, msg.Channel as ITextChannel);
            IPaginatorSenderService paginator = ServiceManager.GetService<IPaginatorSenderService>("Paginator");
            List<LavaTrack> tracks = ply.Queue.Items.ToList();
            if (tracks.Count > 0)
            {
                return await paginator.SendPaginator(msg, "track queue", tracks, async (track, builder) =>
                {
                    int i = tracks.IndexOf(track);
                    builder
                        .WithDescription($"🎶 Track `#{i + 1}` out of `{tracks.Count}` in the queue")
                        .WithField("Title", track.Title)
                        .WithField("Author", track.Author)
                        .WithField("Length", track.IsStream ? " - " : track.Length.ToString(@"hh\:mm\:ss"))
                        .WithField("Stream", track.IsStream);

                    string thumbnailurl = await GetThumbnailAsync(track);
                    if (!string.IsNullOrWhiteSpace(thumbnailurl))
                        builder.WithThumbnailUrl(thumbnailurl);
                });
            }
            return await MessageSender.Good(msg, "track queue", "The track queue is empty");
        }

        private async Task<Embed> GetNewTrackEmbed(LavaTrack lavaTrack, IMessage msg = null)
        {
            string thumbnailUrl = await GetThumbnailAsync(lavaTrack);
            EmbedBuilder builder = new EmbedBuilder();
            if (msg != null)
                builder.WithAuthorNickname(msg);
            string desc = "🎶 Added the following track to the queue:";
            if (!string.IsNullOrWhiteSpace(thumbnailUrl))
                builder.WithThumbnailUrl(thumbnailUrl);
            return builder
                .WithDescription(desc)
                .WithColorType(EmbedColorType.Good)
                .WithFooter("music player")
                .WithField("Title", lavaTrack.Title)
                .WithField("Author", lavaTrack.Author)
                .WithField("Length", lavaTrack.IsStream ? " - " : lavaTrack.Length.ToString(@"hh\:mm\:ss"))
                .WithField("Stream", lavaTrack.IsStream)
                .Build();
        }

        public async Task<IUserMessage> SendNewTrackAsync(IMessage msg, LavaTrack lavaTrack)
        {
            Embed embed = await GetNewTrackEmbed(lavaTrack, msg);

            return await MessageSender.Send(msg, embed);
        }

        public async Task<IUserMessage> SendNewTrackAsync(ITextChannel chan, LavaTrack lavaTrack)
        {
            Embed embed = await GetNewTrackEmbed(lavaTrack);

            return await MessageSender.Send(chan, embed);
        }

        private void AddPlayerReactions(IUserMessage msg, bool isRadio = false)
        {
            Task.Run(async () =>
            {
                if (msg == null) return;

                SocketGuildChannel chan = (SocketGuildChannel)msg.Channel;
                SocketGuild guild = chan.Guild;
                if (guild.CurrentUser.GetPermissions(chan).AddReactions)
                {
                    List<string> unicodeStrings = new List<string> { "⏯", "🔁", "⬆", "⬇", "⏭" };
                    if (isRadio)
                        unicodeStrings.RemoveAt(1);

                    foreach (string unicode in unicodeStrings)
                        await msg.AddReactionAsync(new Emoji(unicode));
                }
            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Logger.Nice("MusicPlayer", ConsoleColor.Yellow, $"Could not create player reactions: {t.Exception.Message}");
            });
        }

        public async Task<IUserMessage> SendPlayerAsync(IEnergizePlayer ply, IQueueObject obj = null, IChannel chan = null)
        {
            obj = obj ?? ply.CurrentTrack;

            if (ply.TrackPlayer == null)
            {
                ply.TrackPlayer = new TrackPlayer(ply.VoiceChannel.GuildId);
                await ply.TrackPlayer.Update(obj, ply.Volume, ply.IsPaused, ply.IsLooping, false);
            }
            else
            {
                await ply.TrackPlayer.Update(obj, ply.Volume, ply.IsPaused, ply.IsLooping, false);
                await ply.TrackPlayer.DeleteMessage();
            }

            if (obj == null) return null;

            ply.TrackPlayer.Message = await MessageSender.Send(chan ?? ply.TextChannel, ply.TrackPlayer.Embed);
            AddPlayerReactions(ply.TrackPlayer.Message, obj is RadioTrack);
            return ply.TrackPlayer.Message;
        }

        private async Task<YoutubeVideo> FetchYTRelatedVideoAsync(string videoId)
        {
            string endpoint = $"https://www.googleapis.com/youtube/v3/search?part=snippet&relatedToVideoId={videoId}&type=video&key={Config.Instance.Keys.YoutubeKey}&maxResults=6";
            string json = await HttpClient.GetAsync(endpoint, Logger);
            YoutubeRelatedVideos relatedVideos = JsonPayload.Deserialize<YoutubeRelatedVideos>(json, Logger);
            if (relatedVideos == null || relatedVideos.Videos.Length == 0) return null;
            IDatabaseService dbService = ServiceManager.GetService<IDatabaseService>("Database");
            using (IDatabaseContext ctx = await dbService.GetContext())
                await ctx.Instance.SaveYoutubeVideoIds(relatedVideos.Videos.Select(vid => vid.Id));

            IEnumerable<YoutubeVideo> vids = relatedVideos.Videos.Where(vid => !vid.Id.VideoID.Equals(videoId));
            return relatedVideos.Videos[Rand.Next(0, relatedVideos.Videos.Length)];
        }

        private static readonly Regex YTRegex = new Regex(@"(?!videoseries)[a-zA-Z0-9_-]{11}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private async Task<(bool, YoutubeVideo)> TryGetVideoAsync(LavaTrack lavaTrack)
        {
            bool failed = false;
            YoutubeVideo video = null;
            if (lavaTrack.Uri.AbsoluteUri.Contains("youtu"))
            {
                Match match = YTRegex.Match(lavaTrack.Uri.AbsoluteUri);
                if (!match.Success)
                    failed = true;

                video = await FetchYTRelatedVideoAsync(match.Value);
                if (video == null)
                    failed = true;
            }
            else
                failed = true;

            return (failed, video);
        }

        private async Task<string> GetNextTrackVideoURLAsync(bool useDb, YoutubeVideo video)
        {
            if (!useDb)
                return $"https://www.youtube.com/watch?v={video.Id.VideoID}";

            IDatabaseService dbService = ServiceManager.GetService<IDatabaseService>("Database");
            using (IDatabaseContext ctx = await dbService.GetContext())
            {
                IYoutubeVideoID videoId = await ctx.Instance.GetRandomVideoIdAsync();
                if (videoId == null) return string.Empty;

                return $"https://www.youtube.com/watch?v={videoId.VideoID.Trim()}";
            }
        }

        private async Task AddRelatedYTContentAsync(IVoiceChannel vc, ITextChannel chan, LavaTrack oldTrack)
        {
            (bool failed, YoutubeVideo video) = await TryGetVideoAsync(oldTrack);
            string videoUrl = await GetNextTrackVideoURLAsync(failed, video);
            SearchResult res = await LavaRestClient.SearchTracksAsync(videoUrl);
            List<LavaTrack> tracks = res.Tracks.ToList();
            if (tracks.Count == 0) return;

            switch (res.LoadType)
            {
                case LoadType.SearchResult:
                case LoadType.TrackLoaded:
                    await AddTrackAsync(vc, chan, tracks[0]);
                    break;
                case LoadType.PlaylistLoaded:
                    await AddPlaylistAsync(vc, chan, res.PlaylistInfo.Name, tracks);
                    break;
                default:
                    await MessageSender.Warning(chan, "music player", "Failed to get/load the next autoplay track");
                    break;
            }
        }

        public async Task<SpotifyTrack> GetSpotifyTrackAsync(string spotifyId)
        {
            FullTrack spotifyResult = await Spotify.GetTrackAsync(spotifyId);
            return new SpotifyTrack(new SpotifyTrackInfo(spotifyResult), () => SearchSpotifyCallback(spotifyResult));
        }

        private async Task<LavaTrack> SearchSpotifyCallback(FullTrack spotifyResult)
        {
            string artistName = spotifyResult.Artists.FirstOrDefault()
                ?.Name + " - " ?? string.Empty;
            SearchResult searchYouTubeAsync = await LavaRestClient.SearchYouTubeAsync($"{artistName}{spotifyResult.Name}");
            return searchYouTubeAsync.Tracks.FirstOrDefault();
        }

        public async Task<IEnumerable<SpotifyTrack>> SearchSpotifyAsync(string search)
        {
            SearchItem searchResult = await Spotify.SearchItemsAsync(search, SearchType.Track);
            Paging<FullTrack> tracks = searchResult.Tracks;
            if (searchResult.HasError())
                return new List<SpotifyTrack>();
            
            return tracks
                .Items
                .Select(spotifyResult =>
                {
                    return new SpotifyTrack(new SpotifyTrackInfo(spotifyResult), () => SearchSpotifyCallback(spotifyResult));
                });
        }

        private async Task OnTrackFinished(LavaPlayer lavalink, LavaTrack lavaTrack, TrackEndReason reason)
        {
            if (reason == TrackEndReason.Cleanup || reason == TrackEndReason.Replaced) return;

            IEnergizePlayer ply = Players[lavalink.VoiceChannel.GuildId];
            if (ply.IsLooping)
            {
                lavaTrack.ResetPosition();
                await ply.Lavalink.PlayAsync(lavaTrack);
            }
            else
            {
                if (ply.Queue.TryDequeue(out LavaTrack newTrack))
                {
                    await ply.Lavalink.PlayAsync(newTrack);
                    await SendPlayerAsync(ply, newTrack);
                }
                else
                {
                    if (ply.Autoplay && ply.Queue.Count == 0)
                        await AddRelatedYTContentAsync(ply.VoiceChannel, ply.TextChannel, lavaTrack);
                    else
                        if (ply.TrackPlayer != null)
                            await ply.TrackPlayer.DeleteMessage();
                }
            }
        }

        private async Task OnTrackIssue(LavaPlayer ply, LavaTrack lavaTrack, string error = null)
        {
            if (error != null)
                Logger.Nice("MusicPlayer", ConsoleColor.Red, $"Exception thrown by lavalink for track <{lavaTrack.Title}>\n{error}");
            else
                Logger.Nice("MusicPlayer", ConsoleColor.Red, $"Track <{lavaTrack.Title}> got stuck");

            EmbedBuilder builder = new EmbedBuilder();
            builder
                .WithColorType(EmbedColorType.Warning)
                .WithFooter("music player")
                .WithDescription("🎶 Could not play track:")
                .WithField("URL", $"**{lavaTrack.Uri}**")
                .WithField("Error", error);

            await MessageSender.Send(ply.TextChannel, builder.Build());
            await SkipTrackAsync(ply.VoiceChannel, ply.TextChannel);
        }

        private delegate Task ReactionCallback(MusicPlayerService music, IEnergizePlayer ply);
        private readonly static Dictionary<string, ReactionCallback> ReactionCallbacks = new Dictionary<string, ReactionCallback>
        {
            ["⏯"] = async (music, ply) =>
            {
                if (!ply.IsPlaying) return;
                if (ply.IsPaused)
                    await music.ResumeTrackAsync(ply.VoiceChannel, ply.TextChannel);
                else
                    await music.PauseTrackAsync(ply.VoiceChannel, ply.TextChannel);
            },
            ["🔁"] = async (music, ply) => await music.LoopTrackAsync(ply.VoiceChannel, ply.TextChannel),
            ["⬆"] = async (music, ply) => await music.SetTrackVolumeAsync(ply.VoiceChannel, ply.TextChannel, ply.Volume + 10),
            ["⬇"] = async (music, ply) => await music.SetTrackVolumeAsync(ply.VoiceChannel, ply.TextChannel, ply.Volume - 10),
            ["⏭"] = async (music, ply) =>
            {
                await ply.TrackPlayer.DeleteMessage();
                await music.SkipTrackAsync(ply.VoiceChannel, ply.TextChannel);
            }
        };

        private bool IsValidReaction(ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (chan is IDMChannel) return false;
            if (reaction.Emote?.Name == null) return false;
            if (reaction.User.GetValueOrDefault() == null) return false;
            if (reaction.User.Value.IsBot || reaction.User.Value.IsWebhook) return false;

            return ReactionCallbacks.ContainsKey(reaction.Emote.Name);
        }

        private bool IsValidTrackPlayer(TrackPlayer trackplayer, ulong msgid)
            => trackplayer != null && trackplayer.Message != null && trackplayer.Message.Id == msgid;

        private async Task OnReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!IsValidReaction(chan, reaction)) return;

            IGuildUser guser = (IGuildUser)reaction.User.Value;
            if (!Players.TryGetValue(guser.GuildId, out IEnergizePlayer ply) || guser.VoiceChannel == null) return;
            if (!IsValidTrackPlayer(ply.TrackPlayer, cache.Id)) return;

            await ReactionCallbacks[reaction.Emote.Name](this, ply);
            if (ply.CurrentRadioLava != null)
                await ply.TrackPlayer.Update(ply.CurrentRadioLava, ply.Volume, ply.IsPaused, ply.IsLooping);
            else
                await ply.TrackPlayer.Update(ply.CurrentTrack, ply.Volume, ply.IsPaused, ply.IsLooping, true);
        }

        [Event("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await OnReaction(cache, chan, reaction);

        [Event("ReactionRemoved")]
        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await OnReaction(cache, chan, reaction);

        private volatile int CurrentShardCount;
        [Event("ShardReady")]
        public async Task OnShardReady(DiscordSocketClient _)
        {
            if (Client.Shards.Count != ++CurrentShardCount) return;

            Configuration config = new Configuration
            {
                ReconnectInterval = TimeSpan.FromSeconds(15),
                ReconnectAttempts = 3,
                Host = Config.Instance.Lavalink.Host,
                Port = Config.Instance.Lavalink.Port,
                Password = Config.Instance.Lavalink.Password,
                SelfDeaf = false,
                BufferSize = 8192,
                PreservePlayers = true,
                AutoDisconnect = false,
                InactivityTimeout = TimeSpan.FromMinutes(3)
            };

            LavaRestClient = new LavaRestClient(config);
            await LavaClient.StartAsync(Client, config);
        }

        private SocketVoiceChannel GetVoiceChannel(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            SocketVoiceChannel vc = oldState.VoiceChannel ?? newState.VoiceChannel;
            SocketGuildUser botUser = vc.Guild.CurrentUser;
            if (oldState.VoiceChannel != botUser.VoiceChannel
                && newState.VoiceChannel != botUser.VoiceChannel) // unrelated channel activities
            {
                return null;
            }

            if (newState.VoiceChannel != null)
            {
                if (user.Id == botUser.Id) // we moved of channel
                    return newState.VoiceChannel;

                if (botUser.VoiceChannel == newState.VoiceChannel) // a user joined our channel
                    return newState.VoiceChannel;
            }
            else
            {
                if (oldState.VoiceChannel != null && user.Id != botUser.Id) // user disconnected
                    return oldState.VoiceChannel;
            }

            return vc;
        }

        private async Task DisconnectTaskAsync(IVoiceChannel vc, CancellationToken token)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), token);

            if (token.IsCancellationRequested)
                return;

            await DisconnectAsync(vc);
        }

        [Event("UserVoiceStateUpdated")] 
        public async Task OnVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            SocketVoiceChannel vc = GetVoiceChannel(user, oldState, newState);
            if (vc == null) return;
            if (!Players.TryGetValue(vc.Guild.Id, out IEnergizePlayer ply)) return;

            if (vc.Users.Count(x => !x.IsBot) < 1)
            {
                ply.DisconnectTask = DisconnectTaskAsync(vc, ply.CTSDisconnect.Token);
            }    
            else
            {
                if (ply.DisconnectTask != null)
                {
                    ply.CTSDisconnect.Cancel(false);
                    ply.CTSDisconnect = new CancellationTokenSource();
                    ply.DisconnectTask = null;
                }
            }
        }
    }
}