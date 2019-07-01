using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using Energize.Services.Listeners.Music.Spotify.Helpers;
using Energize.Services.Listeners.Music.Spotify.Models;
using SpotifyAPI.Web.Models;

namespace Energize.Services.Listeners.Music.Spotify.Providers
{
    internal class SpotifyAlbumProvider : ISpotifyProvider
    {
        public SpotifyRunConfig RunConfig { get; }

        public SpotifyAlbumProvider(SpotifyRunConfig runConfig)
        {
            this.RunConfig = runConfig;
        }

        public async Task<SpotifyCollection> GetAlbumAsync(string id)
        {
            (FullAlbum album, IEnumerable<SpotifyTrackInfo> infos) = await this.GetSpotifyInfos(id, 0, 0);

            return new SpotifyCollection(album, await this.GetTracks(infos));
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

        private async Task<(FullAlbum album, IEnumerable<SpotifyTrackInfo> infos)> GetSpotifyInfos(string id, int startIndex, int maxResults)
        {
            FullAlbum album = await this.RunConfig.Api.GetAlbumAsync( id);

            IEnumerable<SpotifyTrackInfo> sourceTracks = album.Tracks.Items.Select(track => new SpotifyTrackInfo(track));
            IEnumerable<SpotifyTrackInfo> infos = await SpotifyCollectionHandler.GetAllSpotifyInfosAsync(sourceTracks, id, new CollectionOptions(album.Tracks.Total, startIndex, maxResults), this.CollectionGetter);
            return (album, infos);
        }

        private async Task<IEnumerable<SpotifyTrackInfo>> CollectionGetter(string id, CollectionOptions options)
        {
            Paging<SimpleTrack> tracks = await this.RunConfig.Api.GetAlbumTracksAsync(id, limit: options.MaxResults, offset: options.StartIndex);
            return tracks.Items.Select(track => new SpotifyTrackInfo(track));
        }
    }
}