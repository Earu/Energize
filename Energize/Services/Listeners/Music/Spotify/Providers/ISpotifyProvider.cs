using Energize.Services.Listeners.Music.Spotify.Models;

namespace Energize.Services.Listeners.Music.Spotify.Providers
{
    internal interface ISpotifyProvider
    {
        SpotifyRunConfig RunConfig { get; }
    }
}