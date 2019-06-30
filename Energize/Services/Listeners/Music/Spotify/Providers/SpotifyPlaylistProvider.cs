using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energize.Essentials;
using Energize.Essentials.TrackTypes;
using SpotifyAPI.Web;
using Victoria;

namespace Energize.Services.Listeners.Music.Spotify.Providers
{
    internal class SpotifyPlaylistProvider : ISpotifyProvider
    {
        public SpotifyRunConfig RunConfig { get; }

        public SpotifyPlaylistProvider(SpotifyRunConfig runConfig)
        {
            this.RunConfig = runConfig;
        }

        public async Task<SpotifyCollection> GetPlaylistAsync(
            string playlistId,
            int startIndex = 0,
            int maxResults = 0)
        {
            var playlist = await RunConfig.Api.GetPlaylistAsync(null, playlistId);
            IEnumerable<SpotifyTrackInfo> infos =
                playlist.Tracks.Items.Select(playlistTrack => new SpotifyTrackInfo(playlistTrack.Track));
            List<SpotifyTrack> tracks;
            if (RunConfig.Config.LazyLoad)
            {
                tracks = new List<SpotifyTrack>();
                foreach (SpotifyTrackInfo spotifyTrackInfo in infos)
                {
                    tracks.Add(await RunConfig.TrackConverter.CreateSpotifyTrackAsync(spotifyTrackInfo));
                }
            }
            else
            {
                IEnumerable<SpotifyTrack> spotifyTracksAsync = await RunConfig.TrackConverter.CreateSpotifyTracksAsync(infos);
                tracks = spotifyTracksAsync.ToList();
            }

            return new SpotifyCollection(playlist, tracks);
        }
    }
}