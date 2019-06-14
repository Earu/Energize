using Discord;
using Energize.Essentials.MessageConstructs;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        Task DisconnectAllPlayersAsync();

        Task<IUserMessage> AddTrackAsync(IVoiceChannel vc, ITextChannel chan, ITrack track);

        Task<IUserMessage> PlayRadioAsync(IVoiceChannel vc, ITextChannel chan, ITrack track);

        Task<List<IUserMessage>> AddPlaylistAsync(IVoiceChannel vc, ITextChannel chan, string name, IEnumerable<ITrack> tracks);

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

        Task<IUserMessage> SendQueueAsync(IVoiceChannel vc, IMessage msg);

        Task<IUserMessage> SendNewTrackAsync(IMessage msg, ITrack track);

        Task<IUserMessage> SendNewTrackAsync(ITextChannel chan, ITrack track);

        Task<IUserMessage> SendPlayerAsync(IEnergizePlayer ply, IQueueObject obj = null, IChannel chan = null);

        Task<ITrack> ConvertSpotifyTrackToYoutubeAsync(string spotifyId);

        Task<IEnumerable<PaginatorPlayableItem>> SearchSpotifyAsync(string search);
    }
}
