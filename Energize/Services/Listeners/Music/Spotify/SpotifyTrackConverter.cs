using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energize.Essentials;
using Energize.Essentials.TrackTypes;
using SpotifyAPI.Web;
using Victoria;
using Victoria.Entities;

namespace Energize.Services.Listeners.Music.Spotify
{
    internal class SpotifyTrackConverter
    {
        private readonly LavaRestClient _lavaRest;
        protected readonly SpotifyWebAPI Api;
        protected readonly SpotifyConfig Config;

        public SpotifyTrackConverter(LavaRestClient lavaRest, SpotifyWebAPI api, SpotifyConfig config)
        {
            _lavaRest = lavaRest;
            Api = api;
            Config = config;
        }
        
        /// <summary>
        /// Converts a SpotifyTrackInfo to a SpotifyTrack, decides by configuration or parameter if it should search for the InnerTrack now, or LazyLoad (search for it later)
        /// </summary>
        /// <param name="trackInfo">Source SpotifyTrackInfo</param>
        /// <param name="lazyLoad">Optional parameter to specify if to lazy load or not, no matter what the configuration specifies</param>
        /// <returns>Converted SpotifyTrack</returns>
        public async Task<SpotifyTrack> CreateSpotifyTrackAsync(SpotifyTrackInfo trackInfo, bool? lazyLoad = null)
        {
            bool isLazyLoad = lazyLoad ?? Config.LazyLoad;
            return isLazyLoad
                ? new SpotifyTrack(trackInfo, () => SearchSpotify(trackInfo))
                : new SpotifyTrack(trackInfo, await SearchSpotify(trackInfo));
        }

        private async Task<ILavaTrack> SearchSpotify(SpotifyTrackInfo spotifyResult)
        {
            string artistName = $"{spotifyResult.Artists.FirstOrDefault() ?.Name} - ";
            SearchResult searchResult = await _lavaRest.SearchYouTubeAsync($"{artistName}{spotifyResult.Name}");
            return searchResult.Tracks.FirstOrDefault();
        }
    }
}