using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using Energize.Services.Listeners.Music.Spotify.Models;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace Energize.Services.Listeners.Music.Spotify.Providers
{
    internal class SpotifySearchProvider : ISpotifyProvider
    {
        public SpotifyRunConfig RunConfig { get; }
        
        public SpotifySearchProvider(SpotifyRunConfig runConfig)
        {
            this.RunConfig = runConfig;
        }
        
        public async Task<IEnumerable<SpotifyTrack>> SearchAsync(string query, SearchType searchType = SearchType.Track, int maxResults = 0)
        {
            SearchItem searchResult = await this.RunConfig.Api.SearchItemsAsync(query, searchType, maxResults);
            if (searchResult.HasError())
                return new List<SpotifyTrack>();

            IEnumerable<SpotifyTrackInfo> infos = searchResult.Tracks.Items.Select(track => new SpotifyTrackInfo(track));
            if (this.RunConfig.Config.LazyLoad)
            {
                List<SpotifyTrack> tracks = new List<SpotifyTrack>();
                foreach (SpotifyTrackInfo info in infos)
                {
                    tracks.Add(await this.RunConfig.TrackConverter.CreateSpotifyTrackAsync(info, true));
                }

                return tracks;
            }

            return await this.RunConfig.TrackConverter.CreateSpotifyTracksAsync(infos);
        }
    }
}