using Discord;
using Energize.Essentials.MessageConstructs;
using System;
using System.Threading;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;

namespace Energize.Interfaces.Services.Listeners
{
    public interface IEnergizePlayer
    {
        event Action BecameInactive;

        LavaPlayer Lavalink { get; set; }
        Task DisconnectTask { get; set; }
        CancellationTokenSource CTSDisconnect { get; set; }
        bool Autoplay { get; set; }
        bool IsLooping { get; set; }
        bool Disconnected { get; set; }
        TrackPlayer TrackPlayer { get; set; }
        bool IsPlaying { get; }
        bool IsPaused { get; }
        LavaQueue<LavaTrack> Queue { get; }
        LavaTrack CurrentTrack { get; }
        IVoiceChannel VoiceChannel { get; }
        ITextChannel TextChannel { get; }
        int Volume { get; }

        void Refresh();
    }
}
