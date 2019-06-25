using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using Victoria;
using Victoria.Entities;

namespace Energize.Services.Listeners.Music.Spotify
{
    internal class SpotifySearchProvider : SpotifyProviderBase
    {
        public SpotifySearchProvider(
            SpotifyWebAPI api,
            LavaRestClient lavaRest,
            bool lazyLoad) : base(api, lavaRest, lazyLoad)
        {
        }

        public async Task<IEnumerable<SpotifyTrack>> SearchAsync(string query, SearchType searchType = SearchType.All)
        {
            SearchItem searchResult = await this.Api.SearchItemsAsync(query, SearchType.Track);
            Paging<FullTrack> tracks = searchResult.Tracks;
            if (searchResult.HasError())
                return new List<SpotifyTrack>();

            return await ToSpotifyTracks(tracks);
        }

        private async Task<IEnumerable<SpotifyTrack>> ToSpotifyTracks(Paging<FullTrack> tracks)
        {
            List<SpotifyTrack> newTracks = new List<SpotifyTrack>();
            foreach (FullTrack track in tracks.Items)
            {
                newTracks.Add(await CreateSpotifyTrackAsync(new SpotifyTrackInfo(track)));
            }
            return newTracks;
        }
    }
}