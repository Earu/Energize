using System.Threading.Tasks;

namespace Victoria.Entities
{
    public interface IAsyncLazyLoadTrack : ILavaTrack
    {
        Task<ILavaTrack> GetInnerTrackAsync();
    }
}