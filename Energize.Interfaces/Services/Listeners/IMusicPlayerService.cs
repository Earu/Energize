using Discord;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;

namespace Energize.Interfaces.Services.Listeners
{
    public interface IMusicPlayerService : IServiceImplementation
    {
        LavaRestClient LavaRestClient { get; }

        Task<LavaPlayer> ConnectAsync(IVoiceChannel vc, ITextChannel chan);

        Task DisconnectAsync(IVoiceChannel vc);

        Task AddTrack(IVoiceChannel vc, ITextChannel chan, LavaTrack track);

        Task<bool> LoopTrack(IVoiceChannel vc, ITextChannel chan);

        Task ShuffleTracks(IVoiceChannel vc, ITextChannel chan);

        Task ClearTracks(IVoiceChannel vc, ITextChannel chan);

        Task PauseTrack(IVoiceChannel vc, ITextChannel chan);

        Task ResumeTrack(IVoiceChannel vc, ITextChannel chan);

        Task SkipTrack(IVoiceChannel vc, ITextChannel chan);

        Task<IUserMessage> SendQueue(IVoiceChannel vc, IMessage msg);
    }
}
