using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Victoria.Entities;
using Victoria.Entities.Payloads;
using Victoria.Helpers;

namespace Victoria
{
    public abstract class LavaBaseClient
    {
        /// <summary>
        /// Spits out important information.
        /// </summary>
        public event Func<LogMessage, Task> Log
        {
            add { this.ShadowLog += value; }
            remove { this.ShadowLog -= value; }
        }

        /// <summary>
        /// Fires when Lavalink server sends stats.
        /// </summary>
        public event Func<ServerStats, Task> OnServerStats;

        /// <summary>
        /// Fires when Lavalink server closes connection. 
        /// Params are: <see cref="int"/> ErrorCode, <see cref="string"/> Reason, <see cref="bool"/> ByRemote.
        /// </summary>
        public event Func<int, string, bool, Task> OnSocketClosed;

        /// <summary>
        /// Fires when a <see cref="ILavaTrack"/> is stuck. <see cref="long"/> specifies threshold.
        /// </summary>
        public event Func<LavaPlayer, ILavaTrack, long, Task> OnTrackStuck;

        /// <summary>
        /// Fires when <see cref="ILavaTrack"/> throws an exception. <see cref="string"/> is the error reason.
        /// </summary>
        public event Func<LavaPlayer, ILavaTrack, string, Task> OnTrackException;

        /// <summary>
        /// Fires when <see cref="ILavaTrack"/> receives an updated.
        /// </summary>
        public event Func<LavaPlayer, ILavaTrack, TimeSpan, Task> OnPlayerUpdated;

        /// <summary>
        /// Fires when a track has finished playing.
        /// </summary>
        public event Func<LavaPlayer, ILavaTrack, TrackEndReason, Task> OnTrackFinished;

        /// <summary>
        /// Keeps up to date with <see cref="OnServerStats"/>.
        /// </summary>
        public ServerStats ServerStats { get; private set; }

        private BaseSocketClient _baseSocketClient;
        private SocketHelper _socketHelper;
        private Task _disconnectTask;
        private CancellationTokenSource _cancellationTokenSource;
        private Timer _playerUpdateTimer;
        protected Configuration Configuration;
        protected Func<LogMessage, Task> ShadowLog;
        protected ConcurrentDictionary<ulong, LavaPlayer> Players;

        protected async Task InitializeAsync(BaseSocketClient baseSocketClient, Configuration configuration)
        {
            configuration ??= new Configuration();

            this._baseSocketClient = baseSocketClient;
            int shards = baseSocketClient switch 
            { 
                DiscordSocketClient socketClient => await socketClient.GetRecommendedShardCountAsync(), 
                DiscordShardedClient shardedClient => shardedClient.Shards.Count, _ => 1 
            };

            this.Configuration = configuration.SetInternals(baseSocketClient.CurrentUser.Id, shards);
            this.Players = new ConcurrentDictionary<ulong, LavaPlayer>();
            this._cancellationTokenSource = new CancellationTokenSource();
            this._playerUpdateTimer = new Timer(this.OnPlayerUpdateTimer);
            baseSocketClient.UserVoiceStateUpdated += this.OnUserVoiceStateUpdated;
            baseSocketClient.VoiceServerUpdated += this.OnVoiceServerUpdated;

            this._socketHelper = new SocketHelper(configuration, this.ShadowLog);
            this._socketHelper.OnMessage += this.OnMessage;
            this._socketHelper.OnClosed += this.OnClosedAsync;

            await this._socketHelper.ConnectAsync().ConfigureAwait(false);
            this._playerUpdateTimer.Change(0, 1000);
        }

        /// <summary>
        /// Connects to <paramref name="voiceChannel"/> and returns a <see cref="LavaPlayer"/>.
        /// </summary>
        /// <param name="voiceChannel">Voice channel to connect to.</param>
        /// <param name="textChannel">Optional text channel that can send updates.</param>
        public async Task<LavaPlayer> ConnectAsync(IVoiceChannel voiceChannel, ITextChannel textChannel = null)
        {
            if (this.Players.TryGetValue(voiceChannel.GuildId, out LavaPlayer player))
                return player;

            await voiceChannel.ConnectAsync(this.Configuration.SelfDeaf, false, true).ConfigureAwait(false);
            player = new LavaPlayer(voiceChannel, textChannel, this._socketHelper);
            this.Players.TryAdd(voiceChannel.GuildId, player);
            if (this.Configuration.DefaultVolume != 100)
                await player.SetVolumeAsync(this.Configuration.DefaultVolume);

            return player;
        }

        /// <summary>
        /// Disconnects from the <paramref name="voiceChannel"/>.
        /// </summary>
        /// <param name="voiceChannel">Connected voice channel.</param>
        public async Task DisconnectAsync(IVoiceChannel voiceChannel)
        {
            if (!this.Players.TryRemove(voiceChannel.GuildId, out _))
                return;

            await voiceChannel.DisconnectAsync().ConfigureAwait(false);
            DestroyPayload destroyPayload = new DestroyPayload(voiceChannel.GuildId);
            await this._socketHelper.SendPayloadAsync(destroyPayload);
        }

        /// <summary>
        /// Moves voice channels and updates <see cref="LavaPlayer.VoiceChannel"/>.
        /// </summary>
        /// <param name="voiceChannel"><see cref="IVoiceChannel"/></param>
        public async Task MoveChannelsAsync(IVoiceChannel voiceChannel)
        {
            if (!this.Players.TryGetValue(voiceChannel.GuildId, out LavaPlayer player))
                return;

            if (player.VoiceChannel.Id == voiceChannel.Id)
                return;

            await player.PauseAsync();
            await player.VoiceChannel.DisconnectAsync().ConfigureAwait(false);
            await voiceChannel.ConnectAsync(this.Configuration.SelfDeaf, false, true).ConfigureAwait(false);
            await player.ResumeAsync();

            player.VoiceChannel = voiceChannel;
        }

        /// <summary>
        /// Update the <see cref="LavaPlayer.TextChannel"/>.
        /// </summary>
        /// <param name="guildId">Guild Id</param>
        /// <param name="textChannel"><see cref="ITextChannel"/></param>
        public void UpdateTextChannel(ulong guildId, ITextChannel textChannel)
        {
            if (!this.Players.TryGetValue(guildId, out LavaPlayer player))
                return;

            player.TextChannel = textChannel;
        }

        /// <summary>
        /// Gets an existing <see cref="LavaPlayer"/> otherwise null.
        /// </summary>
        /// <param name="guildId">Id of the guild.</param>
        /// <returns><see cref="LavaPlayer"/></returns>
        public LavaPlayer GetPlayer(ulong guildId)
            => this.Players.TryGetValue(guildId, out LavaPlayer player) ? player : default;

        /// <summary>
        /// Enables or disables AutoDisconnect <see cref="Configuration.AutoDisconnect"/>
        /// </summary>
        public void ToggleAutoDisconnect()
            => this.Configuration.AutoDisconnect = !this.Configuration.AutoDisconnect;

        /// <summary>
        /// Disposes all <see cref="LavaPlayer"/>s and closes websocket connection.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            foreach (LavaPlayer player in this.Players.Values)
            {
                await player.DisposeAsync().ConfigureAwait(false);
            }

            this.Players.Clear();
            this.Players = null;
            await this._socketHelper.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        private async Task OnClosedAsync()
        {
            if (this.Configuration.PreservePlayers)
                return;

            foreach (LavaPlayer player in this.Players.Values)
            {
                await this.DisconnectAsync(player.VoiceChannel).ContinueWith(_ => player.DisposeAsync());
            }

            this.Players.Clear();
            this.ShadowLog?.WriteLog(LogSeverity.Warning, "Lavalink died. Disposed all players.");
        }

        private DateTimeOffset NextPlayerUpdate = DateTimeOffset.MinValue;
        private void OnPlayerUpdateTimer(object _)
        {
            try
            {
                DateTimeOffset now = DateTimeOffset.Now;
                bool shouldUpdate = now >= this.NextPlayerUpdate;
                foreach (LavaPlayer player in this.Players.Values)
                {
                    if (player.CurrentTrack != null && player.IsPlaying && !player.IsPaused)
                    {
                        TimeSpan pos;
                        if (player.CurrentTrack.Position == TimeSpan.Zero)
                        {
                            TimeSpan diff = now - player.LastUpdate;
                            pos = player.CurrentTrack.Position.Add(diff);
                        }
                        else
                        {
                            pos = player.CurrentTrack.Position.Add(TimeSpan.FromSeconds(1));
                        }

                        bool hasLen = player.CurrentTrack.HasLength;
                        if (!hasLen)
                            pos = TimeSpan.Zero;
                        else
                            pos = pos >= player.CurrentTrack.Length ? player.CurrentTrack.Length : pos;

                        player.CurrentTrack.Position = pos;
                        player.LastUpdate = now;

                        if (shouldUpdate)
                        {
                            this.NextPlayerUpdate = now.AddSeconds(5);
                            this.OnPlayerUpdated?.Invoke(player, player.CurrentTrack, pos);
                        }

                        if (hasLen && pos >= player.CurrentTrack.Length)
                            this.OnTrackFinished?.Invoke(player, player.CurrentTrack, TrackEndReason.Finished);
                    }
                }
            } 
            catch (Exception ex)
            {
                this.ShadowLog?.WriteLog(LogSeverity.Error, "Could not update players", ex);
            }
        }

        private bool OnMessage(string message)
        {
            this.ShadowLog?.WriteLog(LogSeverity.Debug, message);
            JObject json = JObject.Parse(message);

            ulong guildId = 0;
            LavaPlayer player;

            if (json.TryGetValue("guildId", out JToken guildToken))
                guildId = ulong.Parse($"{guildToken}");

            string opCode = $"{json.GetValue("op")}";
            switch (opCode)
            {
                case "playerUpdate":
                    /*if (!this.Players.TryGetValue(guildId, out player))
                        return false;

                    PlayerState state = json.GetValue("state").ToObject<PlayerState>();
                    player.CurrentTrack.Position = state.Position;
                    player.LastUpdate = state.Time;

                    this.OnPlayerUpdated?.Invoke(player, player.CurrentTrack, state.Position);*/
                    break;

                case "stats":
                    this.ServerStats = json.ToObject<ServerStats>();
                    this.OnServerStats?.Invoke(this.ServerStats);
                    break;

                case "event":
                    EventType evt = json.GetValue("type").ToObject<EventType>();
                    if (!this.Players.TryGetValue(guildId, out player))
                        return false;

                    ILavaTrack track = default;
                    if (json.TryGetValue("track", out JToken hash))
                        track = TrackHelper.DecodeTrack($"{hash}");

                    switch (evt)
                    {
                        case EventType.TrackEnd:
                            TrackEndReason endReason = json.GetValue("reason").ToObject<TrackEndReason>();
                            if (endReason != TrackEndReason.Finished)
                            {
                                if (endReason != TrackEndReason.Replaced)
                                {
                                    player.IsPlaying = false;
                                    player.CurrentTrack = default;
                                }

                                this.OnTrackFinished?.Invoke(player, track, endReason);
                            }

                            break;

                        case EventType.TrackException:
                            string error = json.GetValue("error").ToObject<string>();
                            player.CurrentTrack = track;
                            this.OnTrackException?.Invoke(player, track, error);
                            break;

                        case EventType.TrackStuck:
                            long timeout = json.GetValue("thresholdMs").ToObject<long>();
                            player.CurrentTrack = track;
                            this.OnTrackStuck?.Invoke(player, track, timeout);
                            break;

                        case EventType.WebSocketClosed:
                            string reason = json.GetValue("reason").ToObject<string>();
                            int code = json.GetValue("code").ToObject<int>();
                            bool byRemote = json.GetValue("byRemote").ToObject<bool>();
                            this.OnSocketClosed?.Invoke(code, reason, byRemote);
                            break;

                        default:
                            this.ShadowLog?.WriteLog(LogSeverity.Warning, $"Missing implementation of {evt} event.");
                            break;
                    }

                    break;

                default:
                    this.ShadowLog?.WriteLog(LogSeverity.Warning, $"Missing handling of {opCode} OP code.");
                    break;
            }

            return true;
        }

        private SocketVoiceChannel GetVoiceChannel(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            SocketVoiceChannel channel = oldState.VoiceChannel ?? newState.VoiceChannel;
            SocketGuildUser botUser = channel.Guild.CurrentUser;

            if (oldState.VoiceChannel != botUser.VoiceChannel && newState.VoiceChannel != botUser.VoiceChannel)
                return null;

            switch (newState.VoiceChannel)
            {
                case null:
                    if (oldState.VoiceChannel != null && user.Id != this._baseSocketClient.CurrentUser.Id)
                        return oldState.VoiceChannel;
                    break;

                default:
                    if (user.Id == this._baseSocketClient.CurrentUser.Id)
                        return newState.VoiceChannel;

                    if (botUser.VoiceChannel == newState.VoiceChannel)
                        return newState.VoiceChannel;
                    break;
            }

            return channel;
        }

        private Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            SocketVoiceChannel channel = this.GetVoiceChannel(user, oldState, newState);
            if (channel == null)
                return Task.CompletedTask;

            ulong guildId = channel.Guild.Id;
            if (this.Players.TryGetValue(guildId, out LavaPlayer player) && user.Id == this._baseSocketClient.CurrentUser.Id)
            {
                player.CachedState = newState;
            }

            if (!this.Configuration.AutoDisconnect)
                return Task.CompletedTask;
            
            int users = channel.Users.Count(x => !x.IsBot);

            if (users > 0)
            {
                if (this._disconnectTask is null)
                    return Task.CompletedTask;

                this._cancellationTokenSource.Cancel(false);
                this._cancellationTokenSource = new CancellationTokenSource();
                return Task.CompletedTask;
            }

            if (player is null)
                return Task.CompletedTask;

            this.ShadowLog?.WriteLog(LogSeverity.Warning,
                                $"Automatically disconnecting in {this.Configuration.InactivityTimeout.TotalSeconds} seconds.");

            this._disconnectTask = this.DisconnectTaskAsync(player, this._cancellationTokenSource.Token);
            return Task.CompletedTask;
        }
        
        private async Task DisconnectTaskAsync(LavaPlayer player, CancellationToken token)
        {
            await Task.Delay(this.Configuration.InactivityTimeout, token).ConfigureAwait(false);
            
            if (token.IsCancellationRequested)
                return;
            
            if (player.IsPlaying)
                await player.StopAsync().ConfigureAwait(false);
            
            await this.DisconnectAsync(player.VoiceChannel).ConfigureAwait(false);
        }
        
        private Task OnVoiceServerUpdated(SocketVoiceServer server)
        {
            if (!server.Guild.HasValue || !this.Players.TryGetValue(server.Guild.Id, out LavaPlayer player))
                return Task.CompletedTask;

            VoiceServerPayload update = new VoiceServerPayload(server, player.CachedState.VoiceSessionId);
            return this._socketHelper.SendPayloadAsync(update);
        }
    }
}