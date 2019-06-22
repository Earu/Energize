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

        private Timer TTLTimer;

        internal EnergizePlayer(LavaPlayer ply)
        {
            Lavalink = ply;
            DisconnectTask = null;
            CTSDisconnect = new CancellationTokenSource();
            Autoplay = false;
            IsLooping = false;
            Disconnected = false;
            TrackPlayer = null;
            CurrentRadioLava = null;
            Queue = new LavaQueue<LavaTrack>();
            TimeToLive = 3 * 60 * 1000;
            TTLTimer = null;
            Refresh();
        }

        public LavaPlayer Lavalink { get; set; }

        public Task DisconnectTask { get; set; }
        public CancellationTokenSource CTSDisconnect { get; set; }

        public bool Autoplay { get; set; }
        public bool IsLooping { get; set; }
        public bool Disconnected { get; set; }
        public TrackPlayer TrackPlayer { get; set; }
        public RadioTrack CurrentRadioLava { get; set; }
        public LavaQueue<LavaTrack> Queue { get; private set; }

        public bool IsPlaying { get => Lavalink.IsPlaying; }
        public bool IsPaused { get => Lavalink.IsPaused; }
        public LavaTrack CurrentTrack { get => Lavalink?.CurrentTrack; }
        public IVoiceChannel VoiceChannel { get => Lavalink?.VoiceChannel; }
        public ITextChannel TextChannel { get => Lavalink?.TextChannel; }
        public int Volume { get => Lavalink == null ? 100 : Lavalink.CurrentVolume; }

        public void Refresh()
        {
            if (TTLTimer != null)
            {
                TTLTimer.Stop();
                TTLTimer.Close();
                TTLTimer = null;
            }

            TTLTimer = new Timer(TimeToLive)
            {
                AutoReset = false
            };

            TTLTimer.Elapsed += (_, __) =>
            {
                if (!Disconnected)
                    BecameInactive?.Invoke();
            };

            TTLTimer.Start();
        }
    }
}
