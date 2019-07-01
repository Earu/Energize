using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Victoria.Entities;
using Victoria.Entities.Payloads;
using Victoria.Helpers;
using Victoria.Queue;

namespace Victoria
{
    /// <summary>
    /// Represents a <see cref="IVoiceChannel"/> connection.
    /// </summary>
    public sealed class LavaPlayer
    {
        /// <summary>
        /// Keeps track of <see cref="PauseAsync"/> and <see cref="ResumeAsync"/>.
        /// </summary>
        public bool IsPaused => this._isPaused;

        /// <summary>
        /// Checks whether the <see cref="LavaPlayer"/> is playing or not.
        /// </summary>
        public bool IsPlaying { get; internal set; }

        /// <summary>
        /// Current track that is playing.
        /// </summary>
        public ILavaTrack CurrentTrack { get; internal set; }

        /// <summary>
        /// Optional text channel.
        /// </summary>
        public ITextChannel TextChannel { get; internal set; }

        /// <summary>
        /// Connected voice channel.
        /// </summary>
        public IVoiceChannel VoiceChannel { get; internal set; }

        /// <summary>
        /// Default queue, takes an object that implements <see cref="IQueueObject"/>.
        /// </summary>
        public LavaQueue<IQueueObject> Queue { get; private set; }

        /// <summary>
        /// Last time when Lavalink sent an updated.
        /// </summary>
        public DateTimeOffset LastUpdate { get; internal set; }

        /// <summary>
        /// Keeps track of volume set by <see cref="SetVolumeAsync(int)"/>;
        /// </summary>
        public int CurrentVolume { get; private set; }

        private bool _isPaused;
        private readonly SocketHelper _socketHelper;
        internal SocketVoiceState CachedState;

        private const string INVALID_OP
            = "This operation is invalid since player isn't actually playing anything.";

        internal LavaPlayer(IVoiceChannel voiceChannel, ITextChannel textChannel,
            SocketHelper socketHelper)
        {
            this.VoiceChannel = voiceChannel;
            this.TextChannel = textChannel;
            this._socketHelper = socketHelper;
            this.CurrentVolume = 100;
            this.Queue = new LavaQueue<IQueueObject>();
        }

        /// <summary>
        /// Plays the specified <paramref name="track"/>.
        /// </summary>
        /// <param name="track"><see cref="ILavaTrack"/></param>
        /// <param name="noReplace">If set to true, this operation will be ignored if a track is already playing or paused.</param>
        public async Task PlayAsync(ILavaTrack track, bool noReplace = false)
        {
            this.IsPlaying = true;
            this.CurrentTrack = track;
            if (!noReplace)
                Volatile.Write(ref this._isPaused, false);
            string trackHash = await GetTrackHash(track);
            var payload = new PlayPayload(this.VoiceChannel.GuildId, trackHash, noReplace);
            await this._socketHelper.SendPayloadAsync(payload);
        }
        
        /// <summary>
        /// Plays the specified <paramref name="track"/>.
        /// </summary>
        /// <param name="track"></param>
        /// <param name="startTime">Optional setting that determines the number of milliseconds to offset the track by.</param>
        /// <param name="stopTime">optional setting that determines at the number of milliseconds at which point the track should stop playing.</param>
        /// <param name="noReplace">If set to true, this operation will be ignored if a track is already playing or paused.</param>
        public async Task PlayAsync(ILavaTrack track, TimeSpan startTime, TimeSpan stopTime, bool noReplace = false)
        {
            if (startTime.TotalMilliseconds < 0 || stopTime.TotalMilliseconds < 0)
                throw new InvalidOperationException("Start and stop must be greater than 0.");

            if (startTime <= stopTime)
                throw new InvalidOperationException("Stop time must be greater than start time.");

            this.IsPlaying = true;
            this.CurrentTrack = track;
            if (!noReplace)
                Volatile.Write(ref this._isPaused, false);
            string trackHash = await GetTrackHash(track);
            var payload = new PlayPayload(this.VoiceChannel.GuildId, trackHash, startTime, stopTime, noReplace);
            await this._socketHelper.SendPayloadAsync(payload);
        }

        private static async Task<string> GetTrackHash(ILavaTrack track)
        {
            string trackHash = track.Hash;
            if (track is IAsyncLazyLoadTrack async)
            {
                trackHash = (await async.GetInnerTrackAsync()).Hash;
            }

            return trackHash;
        }
        
        /// <summary>
        /// Stops playing the current track and sets <see cref="IsPlaying"/> to false.
        /// </summary>
        public Task StopAsync()
        {
            if (!this.IsPlaying)
                throw new InvalidOperationException(INVALID_OP);

            this.IsPlaying = false;
            this.CurrentTrack = null;
            Volatile.Write(ref this._isPaused, false);
            var payload = new StopPayload(this.VoiceChannel.GuildId);
            return this._socketHelper.SendPayloadAsync(payload);
        }

        /// <summary>
        /// Resumes if <see cref="IsPaused"/> is set to true.
        /// </summary>
        public Task ResumeAsync()
        {
            if (!this.IsPlaying)
                throw new InvalidOperationException(INVALID_OP);

            Volatile.Write(ref this._isPaused, false);
            var payload = new PausePayload(this.VoiceChannel.GuildId, this.IsPaused);
            return this._socketHelper.SendPayloadAsync(payload);
        }

        /// <summary>
        /// Pauses if <see cref="IsPaused"/> is set to false.
        /// </summary>
        public Task PauseAsync()
        {
            if (!this.IsPlaying)
                throw new InvalidOperationException(INVALID_OP);

            Volatile.Write(ref this._isPaused, true);
            var payload = new PausePayload(this.VoiceChannel.GuildId, this.IsPaused);
            return this._socketHelper.SendPayloadAsync(payload);
        }

        /// <summary>
        /// Replaces the <see cref="CurrentTrack"/> with the next <see cref="ILavaTrack"/> from <see cref="Queue"/>.
        /// </summary>
        /// <returns>Returns the skipped <see cref="ILavaTrack"/>.</returns>
        public async Task<ILavaTrack> SkipAsync()
        {
            if (!this.Queue.TryDequeue(out var item))
                throw new InvalidOperationException($"There are no more items in {nameof(this.Queue)}.");

            if (!(item is ILavaTrack track))
                throw new InvalidCastException($"Couldn't cast {item.GetType()} to {typeof(ILavaTrack)}.");

            var previousTrack = this.CurrentTrack;
            await this.PlayAsync(track);
            return previousTrack;
        }

        /// <summary>
        /// Seeks the <see cref="CurrentTrack"/> to specified <paramref name="position"/>.
        /// </summary>
        /// <param name="position">Position must be less than <see cref="CurrentTrack"/>'s position.</param>
        public Task SeekAsync(TimeSpan position)
        {
            if (!this.IsPlaying)
                throw new InvalidOperationException(INVALID_OP);

            if (position > this.CurrentTrack.Length)
                throw new ArgumentOutOfRangeException($"{nameof(position)} is greater than current track's length.");

            var payload = new SeekPayload(this.VoiceChannel.GuildId, position);
            return this._socketHelper.SendPayloadAsync(payload);
        }

        /// <summary>
        /// Updates <see cref="LavaPlayer"/> volume and updates <see cref="CurrentVolume"/>.
        /// </summary>
        /// <param name="volume">Volume may range from 0 to 1000. 100 is default.</param>
        public Task SetVolumeAsync(int volume)
        {
            if (volume > 1000)
                throw new ArgumentOutOfRangeException($"{nameof(volume)} was greater than max limit which is 1000.");

            this.CurrentVolume = volume;
            var payload = new VolumePayload(this.VoiceChannel.GuildId, volume);
            return this._socketHelper.SendPayloadAsync(payload);
        }

        /// <summary>
        /// Change the <see cref="LavaPlayer"/>'s equalizer. There are 15 bands (0-14) that can be changed.
        /// </summary>
        /// <param name="bands"><see cref="EqualizerBand"/></param>
        public Task EqualizerAsync(List<EqualizerBand> bands)
        {
            if (!this.IsPlaying)
                throw new InvalidOperationException(INVALID_OP);

            var payload = new EqualizerPayload(this.VoiceChannel.GuildId, bands);
            return this._socketHelper.SendPayloadAsync(payload);
        }

        /// <summary>
        /// Change the <see cref="LavaPlayer"/>'s equalizer. There are 15 bands (0-14) that can be changed.
        /// </summary>
        /// <param name="bands"><see cref="EqualizerBand"/></param>
        public Task EqualizerAsync(params EqualizerBand[] bands)
        {
            if (!this.IsPlaying)
                throw new InvalidOperationException(INVALID_OP);

            var payload = new EqualizerPayload(this.VoiceChannel.GuildId, bands);
            return this._socketHelper.SendPayloadAsync(payload);
        }

        internal ValueTask DisposeAsync()
        {
            this.IsPlaying = false;
            this.Queue.Clear();
            this.Queue = null;
            this.CurrentTrack = null;
            GC.SuppressFinalize(this);

            return default;
        }
    }
}
