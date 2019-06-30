using System.Collections.Generic;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace Energize.Services.Listeners.Music.Spotify.Providers
{
    internal class SpotifySearchProvider : ISpotifyProvider
    {
        public SpotifyRunConfig RunConfig { get; }
        
        public SpotifySearchProvider(SpotifyRunConfig runConfig)
        {
            RunConfig = runConfig;
        }

        public async Task<IEnumerable<SpotifyTrack>> SearchAsync(
            string query,
            SearchType searchType = SearchType.All,
            int maxResults = 100)
        {
            SearchItem searchResult = await this.RunConfig.Api.SearchItemsAsync(query, SearchType.Track);
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
                newTracks.Add(await RunConfig.TrackConverter.CreateSpotifyTrackAsync(new SpotifyTrackInfo(track)));
            }
            return newTracks;
        }

    }
}