using System.Threading.Tasks;

namespace Victoria.Entities
{
    /// <summary>
    /// Implements ILavaTrack, used for a track that is not yet searched but has the appropriate metadata for ILavaTrack
    /// <see cref="ILavaTrack" />
    /// </summary>
    public interface IAsyncLazyLoadTrack : ILavaTrack
    {
        Task<ILavaTrack> GetInnerTrackAsync();
    }
}