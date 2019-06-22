using System.Threading.Tasks;
using Victoria.Entities;

namespace Energize.Essentials.TrackTypes
{
    public static class AsyncLavaTrackExtensions
    {
        public static async Task<LavaTrack> ToLavaTrackAsync(this IAsyncLavaTrack asyncTrack)
        {
            return new EasyLavaTrack(
                await asyncTrack.GetIdAsync(),
                await asyncTrack.GetIsSeekableAsync(),
                await asyncTrack.GetAuthorAsync(),
                await asyncTrack.GetIsStreamAsync(),
                await asyncTrack.GetPositionAsync(),
                await asyncTrack.GetLengthAsync(),
                await asyncTrack.GetTitleAsync(),
                await asyncTrack.GetUriAsync(),
                await asyncTrack.GetProviderAsync(),
                async () => await asyncTrack.ResetPositionAsync());
        }
        
    }
}