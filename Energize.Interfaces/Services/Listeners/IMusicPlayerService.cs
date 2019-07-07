using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Energize.Essentials.TrackTypes;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;

namespace Energize.Interfaces.Services.Listeners
{
    public interface IMusicPlayerService : IServiceImplementation
    {
        LavaRestClient LavaRestClient { get; }

        Task<IEnergizePlayer> ConnectAsync(IVoiceChannel vc, ITextChannel chan);

        Task DisconnectAsync(IVoiceChannel vc);

        Task DisconnectAllPlayersAsync(string warnMsg);

        Task<IUserMessage> AddTrackAsync(IVoiceChannel vc, ITextChannel chan, ILavaTrack lavaTrack);
        
        Task<IUserMessage> PlayRadioAsync(IVoiceChannel vc, ITextChannel chan, ILavaTrack lavaTrack);

        Task<List<IUserMessage>> AddPlaylistAsync(IVoiceChannel vc, ITextChannel chan, string name, IEnumerable<ILavaTrack> tracks);

        Task StopTrackAsync(IVoiceChannel vc, ITextChannel chan);

        Task<bool> LoopTrackAsync(IVoiceChannel vc, ITextChannel chan);

        Task<bool> AutoplayTrackAsync(IVoiceChannel vc, ITextChannel chan);

        Task ShuffleTracksAsync(IVoiceChannel vc, ITextChannel chan);

        Task ClearTracksAsync(IVoiceChannel vc, ITextChannel chan);

        Task PauseTrackAsync(IVoiceChannel vc, ITextChannel chan);

        Task ResumeTrackAsync(IVoiceChannel vc, ITextChannel chan);

        Task SkipTrackAsync(IVoiceChannel vc, ITextChannel chan);

        Task SetTrackVolumeAsync(IVoiceChannel vc, ITextChannel chan, int vol);

        Task SeekTrackAsync(IVoiceChannel vc, ITextChannel chan, int amount);

        ServerStats LavalinkStats { get; }

        int PlayerCount { get; }

        int PlayingPlayersCount { get; }

        Task<IUserMessage> SendQueueAsync(IVoiceChannel vc, IMessage msg);

        Task<IUserMessage> SendNewTrackAsync(IMessage msg, ILavaTrack lavaTrack);

        Task<IUserMessage> SendNewTrackAsync(ITextChannel chan, ILavaTrack lavaTrack);

        Task<IUserMessage> SendPlayerAsync(IEnergizePlayer ply, IQueueObject obj = null, IChannel chan = null);

        Task StartAsync(string host = null);
    }
}
