using Discord;
using Energize.Essentials.MessageConstructs;
using System;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;

namespace Energize.Interfaces.Services.Listeners
{
    public interface IEnergizePlayer
    {
        event Action BecameInactive;

        LavaPlayer Lavalink { get; set; }
        bool IsLooping { get; set; }
        TrackPlayer TrackPlayer { get; set; }
        bool IsPlaying { get; }
        bool IsPaused { get; }
        LavaQueue<LavaTrack> Queue { get; }
        LavaTrack CurrentTrack { get; }
        IVoiceChannel VoiceChannel { get; }
        ITextChannel TextChannel { get; }
        int Volume { get; }

        void Refresh(double additionaltime = 0);
    }
}
