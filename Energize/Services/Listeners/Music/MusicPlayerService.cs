using Discord;
using Discord.Net;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Essentials.MessageConstructs;
using Energize.Interfaces.Services.Database;
using Energize.Interfaces.Services.Listeners;
using Energize.Interfaces.Services.Senders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;

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
        private readonly Dictionary<ulong, IEnergizePlayer> Players;
        private readonly Random Rand;

        private bool Initialized;

        public MusicPlayerService(EnergizeClient client)
        {
            this.Initialized = false;
            this.Players = new Dictionary<ulong, IEnergizePlayer>();

            this.Client = client.DiscordClient;
            this.Logger = client.Logger;
            this.MessageSender = client.MessageSender;
            this.ServiceManager = client.ServiceManager;
            this.LavaClient = new LavaShardClient();
            this.Rand = new Random();

            this.LavaClient.OnTrackException += async (ply, track, error) => await this.OnTrackIssue(ply, track, error);
            this.LavaClient.OnTrackStuck += async (ply, track, _) => await this.OnTrackIssue(ply, track);
            this.LavaClient.OnTrackFinished += this.OnTrackFinished;
            this.LavaClient.Log += async (logmsg) => this.Logger.Nice("Lavalink", ConsoleColor.Magenta, logmsg.Message);
            this.LavaClient.OnPlayerUpdated += this.OnPlayerUpdated;
        }

        private async Task OnPlayerUpdated(LavaPlayer lply, LavaTrack track, TimeSpan position)
        {
            if (this.Players.ContainsKey(lply.VoiceChannel.GuildId))
            {
                IEnergizePlayer ply = this.Players[lply.VoiceChannel.GuildId];
                if (ply.TrackPlayer != null && !ply.IsPaused)
                {
                    await ply.TrackPlayer.Update(track, ply.Volume, ply.IsPaused, ply.IsLooping, true);
                    ply.Refresh();
                }
            }

            IGuild guild = lply.VoiceChannel.Guild;
            string msg = $"Updated track <{track.Title}> ({position}) for player in guild <{guild.Name}>";
            this.Logger.LogTo("victoria.log", msg);
        }

        public LavaRestClient LavaRestClient { get; private set; }

        private bool SanitizeCheck(IVoiceChannel vc, ITextChannel chan)
            => vc != null && chan != null;

        public async Task<IEnergizePlayer> ConnectAsync(IVoiceChannel vc, ITextChannel chan)
        {
            if (!this.SanitizeCheck(vc, chan)) return null;

            try
            {
                IEnergizePlayer ply;
                if (this.Players.ContainsKey(vc.GuildId))
                {
                    ply = this.Players[vc.GuildId];
                    if (ply.Lavalink == null) // in case we lose the player object
                        ply.Lavalink = await this.LavaClient.ConnectAsync(vc, chan);
                }
                else
                {
                    ply = new EnergizePlayer(await this.LavaClient.ConnectAsync(vc, chan));
                    this.Logger.Nice("MusicPlayer", ConsoleColor.Magenta, $"Connected to VC in guild {vc.Guild} | {vc.GuildId}");
                    this.Players.Add(vc.GuildId, ply);
                    ply.BecameInactive += async () =>
                    {
                        this.Logger.Nice("MusicPlayer", ConsoleColor.Yellow, $"Connected player became inactive in guild {ply.VoiceChannel.Guild} | {ply.VoiceChannel.GuildId}");
                        await this.DisconnectAsync(vc);
                    };
                }

                this.LavaClient.UpdateTextChannel(vc.GuildId, chan);
                if (vc.Id != ply.Lavalink.VoiceChannel.Id)
                    await this.LavaClient.MoveChannelsAsync(vc);

                return ply;
            }
            catch(ObjectDisposedException)
            {
                this.Logger.Nice("MusicPlayer", ConsoleColor.Red, "Could not connect, threading issue from Discord.NET");
                await this.DisconnectAsync(vc);

                return null;
            }
        }

        public async Task DisconnectAsync(IVoiceChannel vc)
        {
            if (vc == null) return;

            try
            {
                await this.LavaClient.DisconnectAsync(vc);
                if (this.Players.ContainsKey(vc.GuildId))
                {
                    IEnergizePlayer ply = this.Players[vc.GuildId];
                    this.Logger.Nice("MusicPlayer", ConsoleColor.Magenta, $"Disconnected from VC in guild {vc.Guild} | {vc.GuildId}");
                    this.Players.Remove(vc.GuildId);
                    if (ply.TrackPlayer != null)
                        await ply.TrackPlayer.DeleteMessage();
                }
            }
            catch (ObjectDisposedException)
            {
                if (this.Players.ContainsKey(vc.GuildId))
                    this.Players.Remove(vc.GuildId);
                this.Logger.Nice("MusicPlayer", ConsoleColor.Red, "Could not disconnect, threading issue from Discord.NET");
            }
        }

        public async Task DisconnectAllPlayersAsync()
        {
            foreach (KeyValuePair<ulong, IEnergizePlayer> ply in this.Players)
                await this.DisconnectAsync(ply.Value.VoiceChannel);
        }

        public async Task<IUserMessage> AddTrackAsync(IVoiceChannel vc, ITextChannel chan, LavaTrack track)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return null;

            if (ply.IsPlaying)
            {
                ply.Queue.Enqueue(track);
                return await this.SendNewTrackAsync(chan, track);
            }
            else
            {
                await ply.Lavalink.PlayAsync(track, false);
                IUserMessage playerMsg = await this.SendPlayerAsync(ply, track, chan);
                if (ply.Autoplay && ply.Queue.Count == 0 && track.Uri.Host.Contains("youtube"))
                    await this.AddRelatedYTContentAsync(ply.VoiceChannel, ply.TextChannel, track);
                return playerMsg;
            }
        }

        public async Task<List<IUserMessage>> AddPlaylistAsync(IVoiceChannel vc, ITextChannel chan, string name, IEnumerable<LavaTrack> trs)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return null;

            List<LavaTrack> tracks = trs.ToList();
            if (tracks.Count < 1)
                return new List<IUserMessage>
                {
                    await this.MessageSender.Warning(chan, "music player", "The loaded playlist does not contain any tracks")
                };

            if (ply.IsPlaying)
            {
                foreach (LavaTrack track in tracks)
                    ply.Queue.Enqueue(track);

                return new List<IUserMessage>
                {
                    await this.MessageSender.Good(chan, "music player", $"🎶 Added `{tracks.Count}` tracks from `{name}`")
                };
            }
            else
            {
                LavaTrack track = tracks[0];
                tracks.RemoveAt(0);

                if (tracks.Count > 0)
                    foreach (LavaTrack tr in tracks)
                        ply.Queue.Enqueue(tr);

                await ply.Lavalink.PlayAsync(track, false);
                return new List<IUserMessage>
                {
                    await this.MessageSender.Good(chan, "music player", $"🎶 Added `{tracks.Count}` tracks from `{name}`"),
                    await this.SendPlayerAsync(ply, track, chan)
                };
            }
        }

        public async Task<bool> LoopTrackAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return false;

            bool isLooping = ply.IsLooping;
            ply.IsLooping = !isLooping;
            return !isLooping;
        }

        public async Task<bool> AutoplayTrackAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return false;

            bool autoplay = ply.Autoplay;
            ply.Autoplay = !autoplay;

            return !autoplay;
        }

        public async Task ShuffleTracksAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return;

            ply.Queue.Shuffle();
        }

        public async Task ClearTracksAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return;

            ply.Queue.Clear();
        }

        public async Task PauseTrackAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return;

            if (ply.IsPlaying && !ply.IsPaused)
                await ply.Lavalink.PauseAsync();
        }

        public async Task ResumeTrackAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return;

            if (ply.IsPlaying && ply.IsPaused)
                await ply.Lavalink.ResumeAsync();
        }

        public async Task SkipTrackAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return;

            if (ply.IsPlaying)
                await ply.Lavalink.StopAsync();
        }

        public async Task SetTrackVolumeAsync(IVoiceChannel vc, ITextChannel chan, int vol)
        {
            vol = Math.Clamp(vol, 0, 200);
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return;

            if (ply.IsPlaying)
                await ply.Lavalink.SetVolumeAsync(vol);
        }

        public async Task SeekTrackAsync(IVoiceChannel vc, ITextChannel chan, int amount)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return;
            if (!ply.IsPlaying) return;

            LavaTrack track = ply.CurrentTrack;
            TimeSpan total = track.Position.Add(TimeSpan.FromSeconds(amount));
            if (total < track.Length && total >= TimeSpan.Zero)
                await ply.Lavalink.SeekAsync(total);
        }

        public ServerStats LavalinkStats { get => this.LavaClient.ServerStats; }

        public int PlayerCount { get => this.Players.Count; }

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
            IEnergizePlayer ply = await this.ConnectAsync(vc, msg.Channel as ITextChannel);
            IPaginatorSenderService paginator = this.ServiceManager.GetService<IPaginatorSenderService>("Paginator");
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

                    string thumbnailurl = await this.GetThumbnailAsync(track);
                    if (!string.IsNullOrWhiteSpace(thumbnailurl))
                        builder.WithThumbnailUrl(thumbnailurl);
                });
            }
            else
            {
                return await this.MessageSender.Good(msg, "track queue", "The track queue is empty");
            }
        }

        private async Task<Embed> GetNewTrackEmbed(LavaTrack track, IMessage msg = null)
        {
            string thumbnailUrl = await this.GetThumbnailAsync(track);
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
                .WithField("Title", track.Title)
                .WithField("Author", track.Author)
                .WithField("Length", track.IsStream ? " - " : track.Length.ToString(@"hh\:mm\:ss"))
                .WithField("Stream", track.IsStream)
                .Build();
        }

        public async Task<IUserMessage> SendNewTrackAsync(IMessage msg, LavaTrack track)
        {
            Embed embed = await this.GetNewTrackEmbed(track, msg);

            return await this.MessageSender.Send(msg, embed);
        }

        public async Task<IUserMessage> SendNewTrackAsync(ITextChannel chan, LavaTrack track)
        {
            Embed embed = await this.GetNewTrackEmbed(track);

            return await this.MessageSender.Send(chan, embed);
        }

        private void AddPlayerReactions(IUserMessage msg)
        {
            Task.Run(async () =>
            {
                SocketGuildChannel chan = (SocketGuildChannel)msg.Channel;
                SocketGuild guild = chan.Guild;
                if (guild.CurrentUser.GetPermissions(chan).AddReactions)
                {
                    string[] unicodeStrings = new string[] { "⏯", "🔁", "⬆", "⬇", "⏭" };
                    foreach (string unicode in unicodeStrings)
                        await msg.AddReactionAsync(new Emoji(unicode));
                }
            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    this.Logger.Nice("MusicPlayer", ConsoleColor.Yellow, $"Could not create player reactions: {t.Exception.Message}");
            });
        }

        public async Task<IUserMessage> SendPlayerAsync(IEnergizePlayer ply, LavaTrack track = null, IChannel chan = null)
        {
            track = track ?? ply.CurrentTrack;

            if (ply.TrackPlayer == null)
            {
                ply.TrackPlayer = new TrackPlayer(ply.VoiceChannel.GuildId);
                await ply.TrackPlayer.Update(track, ply.Volume, ply.IsPaused, ply.IsLooping, false);
            }
            else
            {
                await ply.TrackPlayer.Update(track, ply.Volume, ply.IsPaused, ply.IsLooping, false);
                await ply.TrackPlayer.DeleteMessage();
            }

            if (track == null) return null;

            ply.TrackPlayer.Message = await this.MessageSender.Send(chan ?? ply.TextChannel, ply.TrackPlayer.Embed);
            this.AddPlayerReactions(ply.TrackPlayer.Message);
            return ply.TrackPlayer.Message;
        }

        private async Task<YoutubeVideo> FetchYTRelatedVideoAsync(string videoId)
        {
            string endpoint = $"https://www.googleapis.com/youtube/v3/search?part=snippet&relatedToVideoId={videoId}&type=video&key={Config.Instance.Keys.YoutubeKey}&maxResults=11";
            string json = await HttpClient.GetAsync(endpoint, this.Logger);
            YoutubeRelatedVideos relatedVideos = JsonPayload.Deserialize<YoutubeRelatedVideos>(json, this.Logger);
            if (relatedVideos == null || relatedVideos.Videos.Length == 0) return null;
            IDatabaseService dbService = this.ServiceManager.GetService<IDatabaseService>("Database");
            using (IDatabaseContext ctx = await dbService.GetContext())
                await ctx.Instance.SaveYoutubeVideoIds(relatedVideos.Videos.Select(vid => vid.Id));

            return relatedVideos.Videos[this.Rand.Next(0, relatedVideos.Videos.Length)];
        }

        private static readonly Regex YTRegex = new Regex(@"https?:\/\/(www\.)?(youtube\.com|youtu\.be)\/(watch\?v=)?([^\/\s&\?]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private async Task AddRelatedYTContentAsync(IVoiceChannel vc, ITextChannel chan, LavaTrack oldTrack)
        {
            bool useSaved = false;
            Match match = YTRegex.Match(oldTrack.Uri.AbsoluteUri);
            if (match == null)
                useSaved = true;

            YoutubeVideo video = await this.FetchYTRelatedVideoAsync(match.Groups[4].Value);
            if (video == null)
                useSaved = true;

            string videoUrl;
            if (useSaved)
            {
                IDatabaseService dbService = this.ServiceManager.GetService<IDatabaseService>("Database");
                using (IDatabaseContext ctx = await dbService.GetContext())
                {
                    IYoutubeVideoID videoId = await ctx.Instance.GetRandomVideoIdAsync();
                    videoUrl = $"https://www.youtube.com/watch?v={videoId.VideoID}";
                }
            }
            else
            {
                videoUrl = $"https://www.youtube.com/watch?v={video.Id.VideoID}";
            }

            SearchResult res = await this.LavaRestClient.SearchTracksAsync(videoUrl);
            List<LavaTrack> tracks = res.Tracks.ToList();
            if (tracks.Count == 0) return;

            switch (res.LoadType)
            {
                case LoadType.SearchResult:
                case LoadType.TrackLoaded:
                    await this.AddTrackAsync(vc, chan, tracks[0]);
                    break;
                case LoadType.PlaylistLoaded:
                    await this.AddPlaylistAsync(vc, chan, res.PlaylistInfo.Name, res.Tracks);
                    break;
            }
        }

        private async Task OnTrackFinished(LavaPlayer lavalink, LavaTrack track, TrackEndReason reason)
        {
            IEnergizePlayer ply = this.Players[lavalink.VoiceChannel.GuildId];
            if (ply.IsLooping)
            {
                track.ResetPosition();
                await ply.Lavalink.PlayAsync(track, false);
            }
            else
            {
                if (ply.Queue.TryDequeue(out LavaTrack newtrack))
                {
                    await ply.Lavalink.PlayAsync(newtrack);
                    await this.SendPlayerAsync(ply, newtrack);
                    if (ply.Autoplay && ply.Queue.Count == 0 && track.Uri.Host.Contains("youtube"))
                        await this.AddRelatedYTContentAsync(ply.VoiceChannel, ply.TextChannel, newtrack);
                }
                else
                {
                    if (ply.TrackPlayer != null)
                        await ply.TrackPlayer.DeleteMessage();
                }
            }
        }

        private async Task OnTrackIssue(LavaPlayer ply, LavaTrack track, string error = null)
        {
            if (error != null)
                this.Logger.Nice("MusicPlayer", ConsoleColor.Red, $"Exception thrown by lavalink for track <{track.Title}>\n{error}");
            else
                this.Logger.Nice("MusicPlayer", ConsoleColor.Red, $"Track <{track.Title}> got stuck");

            EmbedBuilder builder = new EmbedBuilder();
            builder
                .WithColorType(EmbedColorType.Warning)
                .WithFooter("music player")
                .WithDescription("🎶 Could not play track:")
                .WithField("URL", track.Uri)
                .WithField("Error", error);

            await this.MessageSender.Send(ply.TextChannel, builder.Build());
            await this.SkipTrackAsync(ply.VoiceChannel, ply.TextChannel);
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
            },
        };

        private bool IsValidReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (chan is IDMChannel || !cache.HasValue) return false;
            if (reaction.Emote?.Name == null) return false;
            if (reaction.User.Value == null) return false;
            if (reaction.User.Value.IsBot || reaction.User.Value.IsWebhook) return false;

            return ReactionCallbacks.ContainsKey(reaction.Emote.Name);
        }

        private bool IsValidTrackPlayer(TrackPlayer trackplayer, ulong msgid)
            => trackplayer != null && trackplayer.Message != null && trackplayer.Message.Id == msgid;

        private async Task OnReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!this.IsValidReaction(cache, chan, reaction)) return;

            IGuildUser guser = (IGuildUser)reaction.User.Value;
            if (!this.Players.ContainsKey(guser.GuildId) || guser.VoiceChannel == null) return;

            IEnergizePlayer ply = this.Players[guser.GuildId];
            if (!this.IsValidTrackPlayer(ply.TrackPlayer, cache.Id)) return;

            await ReactionCallbacks[reaction.Emote.Name](this, ply);
            await ply.TrackPlayer.Update(ply.CurrentTrack, ply.Volume, ply.IsPaused, ply.IsLooping, true);
        }

        [Event("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);

        [Event("ReactionRemoved")]
        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);

        [Event("ShardReady")]
        public async Task OnShardReady(DiscordSocketClient _)
        {
            if (this.Initialized) return;
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
                InactivityTimeout = TimeSpan.FromMinutes(3),
            };

            this.LavaRestClient = new LavaRestClient(config);
            await this.LavaClient.StartAsync(this.Client, config);
            this.Initialized = true;
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

        [Event("UserVoiceStateUpdated")] // Don't stay in a voice chat if its empty
        public async Task OnVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            SocketVoiceChannel vc = this.GetVoiceChannel(user, oldState, newState);
            if (vc == null) return;
            if (vc.Users.Count(x => !x.IsBot) < 1)
                await this.DisconnectAsync(vc);
        }

        private bool IsDiscordClose(Exception ex)
        {
            if (ex is WebSocketException wsex && wsex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely) return true;
            if (ex is WebSocketClosedException wscex && wscex.CloseCode == 1001) return true;

            return false;
        }

        [Event("ShardDisconnected")] //nasty hack to resume players where they left off
        public async Task OnShardDisconnected(Exception ex, DiscordSocketClient clientShard)
        {
            if (!this.IsDiscordClose(ex)) return;

            this.Logger.Nice("MusicPlayer", ConsoleColor.Red, "Discord sent 1001, and we should have been trying to re-connect");
            /*foreach(KeyValuePair<ulong, IEnergizePlayer> ply in this.Players)
            {
                SocketGuild guild = clientShard.GetGuild(ply.Key);
                if (guild != null)
                {
                    if (!ply.Value.IsPlaying) continue;

                    LavaTrack lastTrack = ply.Value.CurrentTrack;
                    LavaQueue<LavaTrack> lastQueue = ply.Value.Queue;
                    await this.DisconnectAsync(ply.Value.VoiceChannel);
                    IEnergizePlayer newPly = await this.ConnectAsync(ply.Value.VoiceChannel, ply.Value.TextChannel);
                    await newPly.Lavalink.PlayAsync(lastTrack, false);
                    foreach (LavaTrack track in lastQueue.Items)
                        newPly.Queue.Enqueue(track);
                    await this.SendPlayerAsync(newPly, lastTrack);
                }
            }*/
        }
    }
}