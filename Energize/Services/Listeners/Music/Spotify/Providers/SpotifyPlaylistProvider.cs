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
        public SpotifyRunConfig RunConfig { get; }

        public SpotifyPlaylistProvider(SpotifyRunConfig runConfig)
        {
            this.RunConfig = runConfig;
        }

        public async Task<SpotifyCollection> GetPlaylistAsync(string id, int startIndex = 0, int maxResults = 0)
        {
            (FullPlaylist playlist, IEnumerable<SpotifyTrackInfo> infos) = await this.GetSpotifyInfos(id, startIndex, maxResults);

            return new SpotifyCollection(playlist, await this.GetTracks(infos));
        }

        private async Task<List<SpotifyTrack>> GetTracks(IEnumerable<SpotifyTrackInfo> infos)
        {
            List<SpotifyTrack> tracks;
            if (this.RunConfig.Config.LazyLoad)
            {
                tracks = new List<SpotifyTrack>();
                foreach (SpotifyTrackInfo info in infos)
                    tracks.Add(await this.RunConfig.TrackConverter.CreateSpotifyTrackAsync(info, true));
            }
            else
            {
                IEnumerable<SpotifyTrack> spotifyTracksAsync = await this.RunConfig.TrackConverter.CreateSpotifyTracksAsync(infos);
                tracks = spotifyTracksAsync.ToList();
            }

            return tracks;
        }

        private async Task<(FullPlaylist playlist, IEnumerable<SpotifyTrackInfo> infos)> GetSpotifyInfos(string id, int startIndex, int maxResults)
        {
            FullPlaylist playlist = await this.RunConfig.Api.GetPlaylistAsync(null, id);

            IEnumerable<SpotifyTrackInfo> sourceTracks = playlist.Tracks.Items.Select(playlistTrack => new SpotifyTrackInfo(playlistTrack.Track));
            IEnumerable<SpotifyTrackInfo> infos = await SpotifyCollectionHandler.GetAllSpotifyInfosAsync(sourceTracks, id, new CollectionOptions(playlist.Tracks.Total, startIndex, maxResults), this.CollectionGetter);
            return (playlist, infos);
        }

        private async Task<IEnumerable<SpotifyTrackInfo>> CollectionGetter(string id, CollectionOptions options)
        {
            Paging<PlaylistTrack> tracks = await this.RunConfig.Api.GetPlaylistTracksAsync(null, id, limit: options.MaxResults, offset: options.StartIndex);
            return tracks.Items.Select(playlistTrack => new SpotifyTrackInfo(playlistTrack.Track));
        }
    }
}