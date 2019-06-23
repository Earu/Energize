using System;
using System.Threading.Tasks;

namespace Energize.Essentials.TrackTypes
{
    /// <summary>
    /// An interface with asynchronous getters of the public fields of LavaTrack
    /// <see cref="Victoria.Entities.LavaTrack" />
    /// </summary>
    public interface IAsyncTrack
    {
        Task<string> GetIdAsync();
        Task<bool> GetIsSeekableAsync();
        Task<string> GetAuthorAsync();
        Task<bool> GetIsStreamAsync();
        Task<TimeSpan> GetPositionAsync();
        Task<TimeSpan> GetLengthAsync();
        Task<string> GetTitleAsync();
        Task<Uri> GetUriAsync();
        Task<string> GetProviderAsync();

        Task ResetPositionAsync();
    }
}