using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using SpotifyAPI.Web;
using Victoria;

namespace Energize.Services.Listeners.Music.Spotify
{
    internal class SpotifyTrackProvider : SpotifyProviderBase
    {
        public SpotifyTrackProvider(SpotifyWebAPI api, LavaRestClient lavaRest, bool lazyLoad) : base(api, lavaRest, lazyLoad)
        {
        }
        
        public async Task<SpotifyTrack> GetTrackAsync(string id)
        {
            var trackInfo = new SpotifyTrackInfo(await this.Api.GetTrackAsync(id));
            return await this.CreateSpotifyTrackAsync(trackInfo);
        }
    }
}