using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Energize.Essentials.MessageConstructs;
using Energize.Essentials.TrackTypes;
using Energize.Interfaces.Services.Listeners;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;
using Timer = System.Timers.Timer;

namespace Energize.Services.Listeners.Music
{
    // Wrapper class to gain more control over Victoria, and try to prevent weird behaviors
    internal class EnergizePlayer : IEnergizePlayer
    {
        public event Action BecameInactive;

        private readonly double TimeToLive;

        private Timer TtlTimer;

        internal EnergizePlayer(LavaPlayer ply)
        {
            this.Lavalink = ply;
            this.DisconnectTask = null;
            this.CTSDisconnect = new CancellationTokenSource();
            this.Autoplay = false;
            this.IsLooping = false;
            this.Disconnected = false;
            this.TrackPlayer = null;
            this.CurrentRadio = null;
            this.TimeToLive = 3 * 60 * 1000;
            this.TtlTimer = null;
            this.Refresh();
        }

        public LavaPlayer Lavalink { get; set; }

        public Task DisconnectTask { get; set; }
        public CancellationTokenSource CTSDisconnect { get; set; }

        public bool Autoplay { get; set; }
        public bool IsLooping { get; set; }
        public bool Disconnected { get; set; }
        public TrackPlayer TrackPlayer { get; set; }
        public RadioTrack CurrentRadio { get; set; }

        public LavaQueue<IQueueObject> Queue => this.Lavalink.Queue; 
        public bool IsPlaying => this.Lavalink.IsPlaying; 
        public bool IsPaused => this.Lavalink.IsPaused; 
        public ILavaTrack CurrentTrack => this.Lavalink?.CurrentTrack; 
        public IVoiceChannel VoiceChannel => this.Lavalink?.VoiceChannel; 
        public ITextChannel TextChannel => this.Lavalink?.TextChannel; 
        public int Volume => this.Lavalink?.CurrentVolume ?? 100; 

        public void Refresh()
        {
            if (this.TtlTimer != null)
            {
                this.TtlTimer.Stop();
                this.TtlTimer.Close();
                this.TtlTimer = null;
            }

            this.TtlTimer = new Timer(this.TimeToLive)
            {
                AutoReset = false
            };

            this.TtlTimer.Elapsed += (_, __) =>
            {
                if (!this.Disconnected)
                    this.BecameInactive?.Invoke();
            };

            this.TtlTimer.Start();
        }
    }
}
