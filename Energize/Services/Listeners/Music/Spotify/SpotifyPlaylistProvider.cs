using System.Collections.Generic;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;
using Victoria;

namespace Energize.Services.Listeners.Music.Spotify
{
    internal class SpotifyPlaylistProvider : SpotifyProviderBase
    {
        public SpotifyPlaylistProvider(
            SpotifyWebAPI api,
            LavaRestClient lavaRest,
            bool lazyLoad) : base(api, lavaRest, lazyLoad)
        {
        }

        public async Task<SpotifyCollection> GetPlaylistAsync(
            string playlistId,
            int startIndex = 0,
            int maxResults = 100)
        {
            var playlist = await Api.GetPlaylistAsync(null, playlistId);
            var tracks = new List<SpotifyTrack>();
            foreach (PlaylistTrack playlistTrack in playlist.Tracks.Items)
            {
                tracks.Add(await CreateSpotifyTrackAsync(new SpotifyTrackInfo(playlistTrack.Track)));
            }
            return new SpotifyCollection(playlist, tracks);
        }
    }
}