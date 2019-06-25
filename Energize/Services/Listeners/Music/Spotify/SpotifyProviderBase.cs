using System.Linq;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;
using Victoria;
using Victoria.Entities;

namespace Energize.Services.Listeners.Music.Spotify
{
    internal abstract class SpotifyProviderBase
    {
        protected readonly SpotifyWebAPI Api;
        private readonly LavaRestClient _lavaRest;
        protected readonly bool LazyLoad;
        
        public SpotifyProviderBase(SpotifyWebAPI api, LavaRestClient lavaRest, bool lazyLoad)
        {
            Api = api;
            _lavaRest = lavaRest;
            LazyLoad = lazyLoad;
        }

        protected async Task<SpotifyTrack> CreateSpotifyTrackAsync(SpotifyTrackInfo trackInfo)
        {
            return LazyLoad
                ? new SpotifyTrack(trackInfo, () => this.SearchSpotify(trackInfo))
                : new SpotifyTrack(trackInfo, await this.SearchSpotify(trackInfo));
        }
        private async Task<ILavaTrack> SearchSpotify(SpotifyTrackInfo spotifyResult)
        {
            string artistName = $"{spotifyResult.Artists.FirstOrDefault()?.Name} - ";
            SearchResult searchResult = await this._lavaRest.SearchYouTubeAsync($"{artistName}{spotifyResult.Name}");
            return searchResult.Tracks.FirstOrDefault();
        }
    }
}