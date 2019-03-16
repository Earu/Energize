using Victoria;

namespace Energize.Interfaces.Services.Listeners
{
    public interface IMusicPlayerService : IServiceImplementation
    {
        LavaShardClient LavaClient { get; }
        LavaRestClient LavaRestClient { get; }
    }
}
