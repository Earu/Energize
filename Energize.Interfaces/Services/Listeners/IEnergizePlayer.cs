using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Energize.Essentials.MessageConstructs;
using Energize.Essentials.TrackTypes;
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
        bool Inactive { get; set; }
        TrackPlayer TrackPlayer { get; set; }
        bool IsPlaying { get; }
        bool IsPaused { get; }
        RadioTrack CurrentRadio { get; set; }
        LavaQueue<IQueueObject> Queue { get; }
        ILavaTrack CurrentTrack { get; }
        IVoiceChannel VoiceChannel { get; }
        ITextChannel TextChannel { get; }
        int Volume { get; }

        void Refresh();
    }
}
