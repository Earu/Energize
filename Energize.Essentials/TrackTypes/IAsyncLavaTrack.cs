using System;
using System.Threading.Tasks;

namespace Energize.Essentials.TrackTypes
{
    public interface IAsyncLavaTrack
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