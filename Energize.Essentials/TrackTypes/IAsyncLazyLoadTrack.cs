using System.Threading.Tasks;
using Victoria.Entities;

namespace Energize.Essentials.TrackTypes
{
    /// <summary>
    /// An asynchronous track that is lazy loaded, meaning it has an asynchronous getter for its inner LavaTrack
    /// <see cref="IAsyncTrack" />
    /// <seealso cref="Victoria.Entities.LavaTrack" />
    /// </summary>
    public interface IAsyncLazyLoadTrack : IAsyncTrack
    {
        Task<LavaTrack> GetInnerTrackAsync();
    }
}