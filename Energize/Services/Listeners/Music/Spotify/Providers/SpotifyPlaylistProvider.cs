using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using Energize.Services.Listeners.Music.Spotify.Helpers;
using Energize.Services.Listeners.Music.Spotify.Models;
using SpotifyAPI.Web.Models;

namespace Energize.Services.Listeners.Music.Spotify.Providers
{
    internal class SpotifyPlaylistProvider : ISpotifyProvider
    {
        public SpotifyPlaylistProvider(SpotifyRunConfig runConfig)
        {
            RunConfig = runConfig;
        }

        public SpotifyRunConfig RunConfig { get; }

        public async Task<SpotifyCollection> GetPlaylistAsync(string playlistId, int startIndex = 0, int maxResults = 0)
        {
            (FullPlaylist playlist, IEnumerable<SpotifyTrackInfo> infos) =
                await GetSpotifyInfos(playlistId, startIndex, maxResults);

            return new SpotifyCollection(playlist, await GetTracks(infos));
        }

        private async Task<List<SpotifyTrack>> GetTracks(IEnumerable<SpotifyTrackInfo> infos)
        {
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

            return tracks;
        }

        private async Task<(FullPlaylist playlist, IEnumerable<SpotifyTrackInfo> infos)> GetSpotifyInfos(
            string playlistId,
            int startIndex,
            int maxResults)
        {
            FullPlaylist playlist = await RunConfig.Api.GetPlaylistAsync(null, playlistId);

            IEnumerable<SpotifyTrackInfo> sourceTracks =
                playlist.Tracks.Items.Select(playlistTrack => new SpotifyTrackInfo(playlistTrack.Track));
            IEnumerable<SpotifyTrackInfo> infos = await SpotifyCollectionHandler.GetAllSpotifyInfosAsync(
                sourceTracks,
                playlistId,
                new CollectionOptions(playlist.Tracks.Total, startIndex, maxResults),
                CollectionGetter);
            return (playlist, infos);
        }

        private async Task<IEnumerable<SpotifyTrackInfo>> CollectionGetter(string id, CollectionOptions options)
        {
            Paging<PlaylistTrack> playlistTracks = await RunConfig.Api.GetPlaylistTracksAsync(
                null,
                id,
                limit: options.MaxResults,
                offset: options.StartIndex);
            return playlistTracks.Items.Select(playlistTrack => new SpotifyTrackInfo(playlistTrack.Track));
        }
    }
}