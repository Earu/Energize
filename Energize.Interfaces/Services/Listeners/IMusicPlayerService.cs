using Discord;
using System.Collections.Generic;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;

namespace Energize.Interfaces.Services.Listeners
{
    public interface IMusicPlayerService : IServiceImplementation
    {
        LavaRestClient LavaRestClient { get; }

        Task<IEnergizePlayer> ConnectAsync(IVoiceChannel vc, ITextChannel chan);

        Task DisconnectAsync(IVoiceChannel vc);

        Task DisconnectAllPlayersAsync();

        Task<IUserMessage> AddTrackAsync(IVoiceChannel vc, ITextChannel chan, LavaTrack track);

        Task<IUserMessage> AddPlaylistAsync(IVoiceChannel vc, ITextChannel chan, string name, IEnumerable<LavaTrack> tracks);

        Task<bool> LoopTrackAsync(IVoiceChannel vc, ITextChannel chan);

        Task ShuffleTracksAsync(IVoiceChannel vc, ITextChannel chan);

        Task ClearTracksAsync(IVoiceChannel vc, ITextChannel chan);

        Task PauseTrackAsync(IVoiceChannel vc, ITextChannel chan);

        Task ResumeTrackAsync(IVoiceChannel vc, ITextChannel chan);

        Task SkipTrackAsync(IVoiceChannel vc, ITextChannel chan);

        Task SetTrackVolumeAsync(IVoiceChannel vc, ITextChannel chan, int vol);

        Task SeekTrackAsync(IVoiceChannel vc, ITextChannel chan, int amount);

        ServerStats LavalinkStats { get; }

        int PlayerCount { get; }

        Task<IUserMessage> SendQueueAsync(IVoiceChannel vc, IMessage msg);

        Task<IUserMessage> SendNewTrackAsync(IVoiceChannel vc, IMessage msg, LavaTrack track);

        Task<IUserMessage> SendNewTrackAsync(IVoiceChannel vc, ITextChannel chan, LavaTrack track);

        Task<IUserMessage> SendPlayerAsync(IEnergizePlayer ply, LavaTrack track = null, IChannel chan = null);
    }
}
