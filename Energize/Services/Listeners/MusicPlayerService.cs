using Discord;
using Discord.WebSocket;
using Energize.Interfaces.Services.Listeners;
using Energize.Essentials;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;
using System.Linq;
using Energize.Essentials.MessageConstructs;
using Energize.Interfaces.Services.Senders;
using System.Net.WebSockets;
using Discord.Net;

namespace Energize.Services.Listeners
{
    // Wrapper class to gain more control over Victoria, and try to prevent weird behaviors
    internal class EnergizePlayer : IEnergizePlayer
    {
        internal EnergizePlayer(LavaPlayer ply)
        {
            this.Lavalink = ply;
            this.IsLooping = false;
            this.Queue = new LavaQueue<LavaTrack>();
        }

        public LavaPlayer Lavalink { get; set; }

        public bool IsLooping { get; set; }
        public TrackPlayer TrackPlayer { get; set; }
        public LavaQueue<LavaTrack> Queue { get; private set; }

        public bool IsPlaying { get => this.Lavalink.IsPlaying; }
        public bool IsPaused { get => this.Lavalink.IsPaused; }

        public LavaTrack CurrentTrack { get => this.Lavalink?.CurrentTrack; }
        public IVoiceChannel VoiceChannel { get => this.Lavalink?.VoiceChannel; }
        public ITextChannel TextChannel { get => this.Lavalink?.TextChannel; }
        public int Volume { get => this.Lavalink == null ? 100 : this.Lavalink.CurrentVolume; }
    }

    [Service("Music")]
    public class MusicPlayerService : ServiceImplementationBase, IMusicPlayerService
    {
        private readonly DiscordShardedClient _Client;
        private readonly LavaShardClient _LavaClient;
        private readonly Logger _Logger;
        private readonly MessageSender _MessageSender;
        private readonly ServiceManager _ServiceManager;
        private readonly Dictionary<ulong, IEnergizePlayer> _Players;

        private bool _Initialized;

        public MusicPlayerService(EnergizeClient client)
        {
            this._Initialized = false;
            this._Players = new Dictionary<ulong, IEnergizePlayer>();

            this._Client = client.DiscordClient;
            this._Logger = client.Logger;
            this._MessageSender = client.MessageSender;
            this._ServiceManager = client.ServiceManager;
            this._LavaClient = new LavaShardClient();

            this._LavaClient.OnTrackException += async (ply, track, error) => await this.OnTrackIssue(ply, track, error);
            this._LavaClient.OnTrackStuck += async (ply, track, _) => await this.OnTrackIssue(ply, track);
            this._LavaClient.OnTrackFinished += this.OnTrackFinished;
            this._LavaClient.Log += async (logmsg) => this._Logger.Nice("Lavalink", ConsoleColor.Magenta, logmsg.Message);
            this._LavaClient.OnPlayerUpdated += this.OnPlayerUpdated;
        }

        private async Task OnPlayerUpdated(LavaPlayer lply, LavaTrack track, TimeSpan position)
        {
            if (this._Players.ContainsKey(lply.VoiceChannel.GuildId))
            {
                IEnergizePlayer ply = this._Players[lply.VoiceChannel.GuildId];
                if (ply.TrackPlayer != null && !ply.IsPaused)
                    await ply.TrackPlayer.Update(track, ply.Volume, ply.IsPaused, ply.IsLooping, true);
            }

            IGuild guild = lply.VoiceChannel.Guild;
            string msg = $"Updated track <{track.Title}> ({position}) for player in guild <{guild.Name}>";
            this._Logger.LogTo("victoria.log", msg);
        }

        public LavaRestClient LavaRestClient { get; private set; }

        private bool SanitizeCheck(IVoiceChannel vc, ITextChannel chan)
            => vc != null && chan != null;

        public async Task<IEnergizePlayer> ConnectAsync(IVoiceChannel vc, ITextChannel chan)
        {
            if (!this.SanitizeCheck(vc, chan)) return null;

            IEnergizePlayer ply;
            if (this._Players.ContainsKey(vc.GuildId))
            {
                ply = this._Players[vc.GuildId];
                if (ply.Lavalink == null) // in case we lose the player object
                    ply.Lavalink = await this._LavaClient.ConnectAsync(vc, chan);
            }
            else
            {
                ply = new EnergizePlayer(await this._LavaClient.ConnectAsync(vc, chan));
                this._Players.Add(vc.GuildId, ply);
            }

            if (vc.Id != ply.Lavalink.VoiceChannel.Id)
                await this._LavaClient.MoveChannelsAsync(vc);

            return ply;
        }

        public async Task DisconnectAsync(IVoiceChannel vc)
        {
            if (vc == null) return;

            await this._LavaClient.DisconnectAsync(vc);
            if (this._Players.ContainsKey(vc.GuildId))
            {
                IEnergizePlayer ply = this._Players[vc.GuildId];
                this._Players.Remove(vc.GuildId);
                if (ply.TrackPlayer != null)
                    await ply.TrackPlayer.DeleteMessage();
            }
        }

        public async Task DisconnectAllPlayersAsync()
        {
            foreach (KeyValuePair<ulong, IEnergizePlayer> ply in this._Players)
                await this._LavaClient.DisconnectAsync(ply.Value.VoiceChannel);
        }

        public async Task<IUserMessage> AddTrackAsync(IVoiceChannel vc, ITextChannel chan, LavaTrack track)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return null;

            if (ply.IsPlaying)
            {
                ply.Queue.Enqueue(track);
                return await this.SendNewTrackAsync(vc, chan, track);
            }
            else
            {
                await ply.Lavalink.PlayAsync(track, false);
                return await this.SendPlayerAsync(ply, track);
            }
        }

        public async Task<IUserMessage> AddPlaylistAsync(IVoiceChannel vc, ITextChannel chan, string name, IEnumerable<LavaTrack> trs)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return null;

            List<LavaTrack> tracks = trs.ToList();
            if (ply.IsPlaying)
            {
                foreach (LavaTrack track in tracks)
                    ply.Queue.Enqueue(track);

                return await this._MessageSender.Good(chan, "music player", $"🎶 Added `{tracks.Count}` tracks from `{name}");
            }
            else
            {
                if (tracks.Count > 0)
                    return await this._MessageSender.Warning(chan, "music player", "The loaded playlist does not contain any tracks");

                LavaTrack track = tracks[0];
                tracks.RemoveAt(0);

                if (tracks.Count > 0)
                    foreach (LavaTrack tr in tracks)
                        ply.Queue.Enqueue(tr);

                await ply.Lavalink.PlayAsync(track, false);
                return await this.SendPlayerAsync(ply, track);
            }
        }

        public async Task<bool> LoopTrackAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return false;

            bool islooping = ply.IsLooping;
            ply.IsLooping = !islooping;
            return !islooping;
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
            if (ply.IsPlaying)
                await ply.Lavalink.StopAsync();
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

        public async Task<string> GetTrackLyricsAsync(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply == null) return string.Empty;

            if (ply.IsPlaying)
            {
                LavaTrack track = ply.CurrentTrack;
                return await track.FetchLyricsAsync();
            }

            return "Nothing is playing";
        }

        public ServerStats LavalinkStats { get => this._LavaClient.ServerStats; }

        public int PlayerCount { get => this._Players.Count; }

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
            IPaginatorSenderService paginator = this._ServiceManager.GetService<IPaginatorSenderService>("Paginator");
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
                return await this._MessageSender.Good(msg, "track queue", "The track queue is empty");
            }
        }

        private async Task<Embed> GetNewTrackEmbed(LavaTrack track, IMessage msg = null)
        {
            string thumbnailurl = await this.GetThumbnailAsync(track);
            EmbedBuilder builder = new EmbedBuilder();
            if (msg != null)
                builder.WithAuthorNickname(msg);
            string desc = "🎶 Added the following track to the queue:";
            if (!string.IsNullOrWhiteSpace(thumbnailurl))
                builder.WithThumbnailUrl(thumbnailurl);
            return builder
                .WithDescription(desc)
                .WithColor(this._MessageSender.ColorGood)
                .WithFooter("music player")
                .WithField("Title", track.Title)
                .WithField("Author", track.Author)
                .WithField("Length", track.IsStream ? " - " : track.Length.ToString(@"hh\:mm\:ss"))
                .WithField("Stream", track.IsStream)
                .Build();
        }

        public async Task<IUserMessage> SendNewTrackAsync(IVoiceChannel vc, IMessage msg, LavaTrack track)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, msg.Channel as ITextChannel);
            Embed embed = await this.GetNewTrackEmbed(track, msg);

            return await this._MessageSender.Send(ply.TextChannel, embed);
        }

        public async Task<IUserMessage> SendNewTrackAsync(IVoiceChannel vc, ITextChannel chan, LavaTrack track)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            Embed embed = await this.GetNewTrackEmbed(track);

            return await this._MessageSender.Send(ply.TextChannel, embed);
        }

        private void AddPlayerReactions(IUserMessage msg)
        {
            Task.Run(async () =>
            {
                SocketGuildChannel chan = (SocketGuildChannel)msg.Channel;
                SocketGuild guild = chan.Guild;
                if (guild.CurrentUser.GetPermissions(chan).AddReactions)
                {
                    string[] unicodestrings = new string[] { "⏯", "🔁", "⬆", "⬇", "⏭" };
                    foreach (string unicode in unicodestrings)
                        await msg.AddReactionAsync(new Emoji(unicode));
                }
            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    this._Logger.Nice("MusicPlayer", ConsoleColor.Yellow, $"Could not create player reactions: {t.Exception.Message}");
            });
        }

        public async Task<IUserMessage> SendPlayerAsync(IEnergizePlayer ply, LavaTrack track = null)
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

            ply.TrackPlayer.Message = await this._MessageSender.Send(ply.TextChannel, ply.TrackPlayer.Embed);
            this.AddPlayerReactions(ply.TrackPlayer.Message);
            return ply.TrackPlayer.Message;
        }

        private async Task OnTrackFinished(LavaPlayer lavalink, LavaTrack track, TrackEndReason reason)
        {
            IEnergizePlayer ply = this._Players[lavalink.VoiceChannel.GuildId];
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
                this._Logger.Nice("MusicPlayer", ConsoleColor.Red, $"Exception thrown by lavalink for track <{track.Title}>\n{error}");
            else
                this._Logger.Nice("MusicPlayer", ConsoleColor.Red, $"Track <{track.Title}> got stuck");

            EmbedBuilder builder = new EmbedBuilder();
            builder
                .WithColor(this._MessageSender.ColorWarning)
                .WithFooter("music player")
                .WithDescription("🎶 There was a problem playing the following track")
                .WithField("Title", track.Title)
                .WithField("Author", track.Author)
                .WithField("Length", track.IsStream ? " - " : track.Length.ToString(@"hh\:mm\:ss"))
                .WithField("Stream", track.IsStream)
                .WithField("Error", $"`{error ?? "The track got stuck"}`", false);

            string thumbnailurl = await this.GetThumbnailAsync(track);
            if (!string.IsNullOrWhiteSpace(thumbnailurl))
                builder.WithThumbnailUrl(thumbnailurl);

            await this._MessageSender.Send(ply.TextChannel, builder.Build());
            await this.SkipTrackAsync(ply.VoiceChannel, ply.TextChannel);
        }

        private delegate Task ReactionCallback(MusicPlayerService music, IEnergizePlayer ply);
        private readonly static Dictionary<string, ReactionCallback> _ReactionCallbacks = new Dictionary<string, ReactionCallback>
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
            return _ReactionCallbacks.ContainsKey(reaction.Emote.Name);
        }

        private bool IsValidTrackPlayer(TrackPlayer trackplayer, ulong msgid)
            => trackplayer != null && trackplayer.Message != null && trackplayer.Message.Id == msgid;

        private async Task OnReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!this.IsValidReaction(cache, chan, reaction)) return;

            IGuildUser guser = (IGuildUser)reaction.User.Value;
            if (!this._Players.ContainsKey(guser.GuildId) || guser.VoiceChannel == null) return;

            IEnergizePlayer ply = this._Players[guser.GuildId];
            if (!this.IsValidTrackPlayer(ply.TrackPlayer, cache.Id)) return;

            await _ReactionCallbacks[reaction.Emote.Name](this, ply);
            await ply.TrackPlayer.Update(ply.CurrentTrack, ply.Volume, ply.IsPaused, ply.IsLooping, true);
        }

        [Event("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);

        [Event("ReactionRemoved")]
        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);

        [Event("UserVoiceStateUpdated")] // Don't stay in a voice chat if its empty
        public async Task OnVoiceStateUpdated(SocketUser user, SocketVoiceState oldstate, SocketVoiceState newstate)
        {
            SocketVoiceChannel vc = null;
            SocketGuildUser guser = (SocketGuildUser)user;
            SocketGuildUser botuser = guser.Guild.CurrentUser;
            if (botuser == null)
            {
                vc = oldstate.VoiceChannel ?? newstate.VoiceChannel;
            }
            else
            {
                if (user.Id == Config.Instance.Discord.BotID && oldstate.VoiceChannel != null && newstate.VoiceChannel != null) // bot moved of channel
                    vc = newstate.VoiceChannel;
                else if (oldstate.VoiceChannel != null && newstate.VoiceChannel != null && newstate.VoiceChannel == botuser.VoiceChannel) // user moved to our channel
                    vc = newstate.VoiceChannel;
                else if (botuser.VoiceChannel != null && oldstate.VoiceChannel != botuser.VoiceChannel && newstate.VoiceChannel != botuser.VoiceChannel) // other channels activies
                    vc = null;
                else // dc and other behaviors
                    vc = oldstate.VoiceChannel ?? newstate.VoiceChannel;
            }

            if (vc == null) return;
            if (vc.Users.Count(x => !x.IsBot) < 1)
                await this.DisconnectAsync(vc);
        }

        [Event("ShardReady")]
        public async Task OnShardReady(DiscordSocketClient _)
        {
            if (this._Initialized) return;
            Configuration config = new Configuration
            {
                ReconnectInterval = TimeSpan.FromSeconds(15.0),
                ReconnectAttempts = 3,
                Host = Config.Instance.Lavalink.Host,
                Port = Config.Instance.Lavalink.Port,
                Password = Config.Instance.Lavalink.Password,
                SelfDeaf = false,
                BufferSize = 8192,
                PreservePlayers = true,
            };

            this.LavaRestClient = new LavaRestClient(config);
            await this._LavaClient.StartAsync(this._Client, config);
            this._Initialized = true;
        }

        private bool IsDiscordClose(Exception ex)
        {
            if (ex is WebSocketException wsex && wsex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely) return true;
            if (ex is WebSocketClosedException wscex && wscex.CloseCode == 1001) return true;

            return false;
        }

        [Event("ShardDisconnected")] //nasty hack to resume players where they left off
        public async Task OnShardDisconnected(Exception ex, DiscordSocketClient clientshard)
        {
            if (!this.IsDiscordClose(ex)) return;

            foreach(KeyValuePair<ulong, IEnergizePlayer> ply in this._Players)
            {
                SocketGuild guild = clientshard.GetGuild(ply.Key);
                if (guild != null && ply.Value.TextChannel != null)
                {
                    if (!ply.Value.IsPlaying) continue;

                    LavaTrack lasttrack = ply.Value.CurrentTrack;
                    LavaQueue<LavaTrack> lastqueue = ply.Value.Queue;
                    await this.DisconnectAsync(ply.Value.VoiceChannel);
                    IEnergizePlayer newply = await this.ConnectAsync(ply.Value.VoiceChannel, ply.Value.TextChannel);
                    await newply.Lavalink.PlayAsync(lasttrack, false);
                    foreach (LavaTrack track in lastqueue.Items)
                        newply.Queue.Enqueue(track);
                    await this.SendPlayerAsync(newply, lasttrack);
                }
            }
        }
    }
}