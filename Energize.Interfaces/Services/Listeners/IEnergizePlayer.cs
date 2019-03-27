using Discord;
using Energize.Essentials.MessageConstructs;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;

namespace Energize.Interfaces.Services.Listeners
{
    public interface IEnergizePlayer
    {
        LavaPlayer Lavalink { get; }
        bool IsLooping { get; set; }
        TrackPlayer TrackPlayer { get; set; }
        bool IsPlaying { get; }
        bool IsPaused { get; }
        LavaQueue<IQueueObject> Queue { get; }
        LavaTrack CurrentTrack { get; }
        IVoiceChannel VoiceChannel { get; }
        ITextChannel TextChannel { get; }
        int Volume { get; }
    }
}
