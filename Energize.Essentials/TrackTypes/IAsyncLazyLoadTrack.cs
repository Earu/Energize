using System.Threading.Tasks;
using Victoria.Entities;

namespace Energize.Essentials.TrackTypes
{
    public interface IAsyncLazyLoadTrack : IAsyncLavaTrack
    {
        Task<LavaTrack> GetInnerTrackAsync();
    }
}