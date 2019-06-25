using System.Collections.Generic;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using Energize.Interfaces.Services.Listeners;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Enums;
using Victoria;

namespace Energize.Services.Listeners.Music.Spotify
{
    public class SpotifyHandlerService : ISpotifyHandlerService
    {
        private readonly SpotifyWebAPI _api;
        private readonly LavaRestClient _lavaRest;
        private readonly bool _lazyLoad;
        
        private readonly SpotifySearchProvider _searchProvider;

        public SpotifyHandlerService(
            SpotifyWebAPI api,
            LavaRestClient lavaRest,
            bool lazyLoad)
        {
            _api = api;
            _lavaRest = lavaRest;
            _lazyLoad = lazyLoad;
            
            _searchProvider = new SpotifySearchProvider(_api, _lavaRest, _lazyLoad);
        }
        public Task<IEnumerable<SpotifyTrack>> SearchAsync(string query, SearchType searchType = SearchType.All) 
            => _searchProvider.SearchAsync(query, searchType);

        public Task<SpotifyTrack> GetTrackAsync(string id)
        {
            throw new System.NotImplementedException();
        }
    }
}