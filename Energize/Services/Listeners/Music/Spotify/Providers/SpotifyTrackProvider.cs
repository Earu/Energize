using System.Threading.Tasks;
using Energize.Essentials;
using Energize.Essentials.TrackTypes;
using SpotifyAPI.Web;
using Victoria;

namespace Energize.Services.Listeners.Music.Spotify.Providers
{
    internal class SpotifyTrackProvider : ISpotifyProvider
    {
        public SpotifyRunConfig RunConfig { get; }

        public SpotifyTrackProvider(SpotifyRunConfig runConfig)
        {
            RunConfig = runConfig;
        }

        public async Task<SpotifyTrack> GetTrackAsync(string id)
        {
            var trackInfo = new SpotifyTrackInfo(await this.RunConfig.Api.GetTrackAsync(id));
            return await this.RunConfig.TrackConverter.CreateSpotifyTrackAsync(trackInfo);
        }
    }
}