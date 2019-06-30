using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using Energize.Services.Listeners.Music.Spotify.Helpers;
using SpotifyAPI.Web.Models;

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
            //var playlist = await RunConfig.Api.GetPlaylistAsync(null, playlistId, "tracks(total),owner,description,uri,name");
            var playlist = await RunConfig.Api.GetPlaylistAsync(null, playlistId);


            var sourceTracks = playlist.Tracks.Items.Select(playlistTrack => new SpotifyTrackInfo(playlistTrack.Track));
            var infos = await SpotifyCollectionHandler.GetAllSpotifyInfosAsync(
                sourceTracks,
                playlistId,
                new CollectionOptions(playlist.Tracks.Total, startIndex, maxResults),
                CollectionGetter);
                
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

        private async Task<IEnumerable<SpotifyTrackInfo>> CollectionGetter(string id, CollectionOptions options)
        {
            var playlistTracks = await RunConfig.Api.GetPlaylistTracksAsync(null, id, limit: options.MaxResults, offset: options.StartIndex);
            return playlistTracks.Items.Select(playlistTrack => new SpotifyTrackInfo(playlistTrack.Track));
        }
    }
}