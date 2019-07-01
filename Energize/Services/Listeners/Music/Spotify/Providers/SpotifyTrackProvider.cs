using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using Energize.Services.Listeners.Music.Spotify.Models;

namespace Energize.Services.Listeners.Music.Spotify.Providers
{
    internal class SpotifyTrackProvider : ISpotifyProvider
    {
        public SpotifyRunConfig RunConfig { get; }

        public SpotifyTrackProvider(SpotifyRunConfig runConfig)
        {
            this.RunConfig = runConfig;
        }

        public async Task<SpotifyTrack> GetTrackAsync(string id)
        {
            SpotifyTrackInfo trackInfo = new SpotifyTrackInfo(await this.RunConfig.Api.GetTrackAsync(id));
            return await this.RunConfig.TrackConverter.CreateSpotifyTrackAsync(trackInfo);
        }
    }
}