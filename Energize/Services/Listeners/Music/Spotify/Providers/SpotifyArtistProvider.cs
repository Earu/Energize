using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using Energize.Services.Listeners.Music.Spotify.Models;
using SpotifyAPI.Web.Models;

namespace Energize.Services.Listeners.Music.Spotify.Providers
{
    internal class SpotifyArtistProvider : ISpotifyProvider
    {
        public SpotifyRunConfig RunConfig { get; }

        public SpotifyArtistProvider(SpotifyRunConfig runConfig)
        {
            this.RunConfig = runConfig;
        }

        public async Task<(string name, Uri uri)> GetArtistAsync(string id)
        {
            FullArtist artist = await this.RunConfig.Api.GetArtistAsync(id);
            return (artist.Name, new Uri(artist.Uri));
        }

        public async Task<IEnumerable<SpotifyTrack>> GetArtistTopTracksAsync(string id, string country = "US")
        {
            SeveralTracks artistsTopTracksAsync = await this.RunConfig.Api.GetArtistsTopTracksAsync(id, country);
            IEnumerable<SpotifyTrackInfo> infos = artistsTopTracksAsync.Tracks.Select(track => new SpotifyTrackInfo(track));

            if (this.RunConfig.Config.LazyLoad)
            {
                List<SpotifyTrack> tracks = new List<SpotifyTrack>();
                foreach (SpotifyTrackInfo info in infos)
                    tracks.Add(await this.RunConfig.TrackConverter.CreateSpotifyTrackAsync(info, true));

                return tracks;
            }

            return await this.RunConfig.TrackConverter.CreateSpotifyTracksAsync(infos);
        }
    }
}