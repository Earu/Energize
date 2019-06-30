using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Energize.Essentials;
using Energize.Essentials.TrackTypes;
using Energize.Interfaces.Services.Listeners;
using Energize.Services.Listeners.Music.Spotify.Helpers;
using Energize.Services.Listeners.Music.Spotify.Models;
using Energize.Services.Listeners.Music.Spotify.Providers;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Enums;
using Victoria;

namespace Energize.Services.Listeners.Music.Spotify
{
    /// <inheritdoc cref="ISpotifyHandlerService" />
    [Service("Spotify")]
    public class SpotifyHandlerService : ServiceImplementationBase, ISpotifyHandlerService
    {
        private readonly Logger _logger;
        private readonly SpotifyWebAPI _api;
        private readonly LavaRestClient _lavaRest;
        private readonly SpotifyConfig _config;

        private readonly Timer _spotifyAuthTimer;
        private readonly SpotifyTrackProvider _trackProvider;
        private readonly SpotifySearchProvider _searchProvider;
        private readonly SpotifyPlaylistProvider _playlistProvider;
        private readonly SpotifyAlbumProvider _albumProvider;
        private readonly SpotifyArtistProvider _artistProvider;


        public SpotifyHandlerService(EnergizeClient client)
        {
            _logger = client.Logger;
            _api = new SpotifyWebAPI
            {
                TokenType = "Bearer",
                UseAuth = true,
                UseAutoRetry = true
            };
            _lavaRest = GetLavaRestClient();
            // TODO: add configuration entry
            _config = Config.Instance.Spotify;
            _spotifyAuthTimer = new Timer(TradeSpotifyToken);

            var spotifyRunConfig = new SpotifyRunConfig(_lavaRest, _api, _config, new SpotifyTrackConverter(_lavaRest, _config));
            _trackProvider = new SpotifyTrackProvider(spotifyRunConfig);
            _searchProvider = new SpotifySearchProvider(spotifyRunConfig);
            _playlistProvider = new SpotifyPlaylistProvider(spotifyRunConfig);
            _albumProvider = new SpotifyAlbumProvider(spotifyRunConfig);
            _artistProvider = new SpotifyArtistProvider(spotifyRunConfig);
        }

        private static LavaRestClient GetLavaRestClient()
        {
            var config = new Configuration
            {
                ReconnectInterval = TimeSpan.FromSeconds(15),
                ReconnectAttempts = 3,
                Host = Config.Instance.Lavalink.Host,
                Port = Config.Instance.Lavalink.Port,
                Password = Config.Instance.Lavalink.Password,
                SelfDeaf = false,
                BufferSize = 8192,
                PreservePlayers = true,
                AutoDisconnect = false,
                LogSeverity = LogSeverity.Debug,
                DefaultVolume = 50,
                InactivityTimeout = TimeSpan.FromMinutes(3)
            };

            return new LavaRestClient(config);
        }

        private async void TradeSpotifyToken(object _)
        {
            void Callback(HttpWebRequest req)
            {
                byte[] credBytes = Encoding.UTF8.GetBytes($"{_config.ClientID}:{_config.ClientSecret}");
                req.Headers[HttpRequestHeader.Authorization] = $"Basic {Convert.ToBase64String(credBytes)}";
                req.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            }

            string json = await HttpClient.PostAsync(
                "https://accounts.spotify.com/api/token?grant_type=client_credentials",
                string.Empty,
                _logger,
                null,
                Callback);

            Dictionary<string, string> keys = JsonPayload.Deserialize<Dictionary<string, string>>(json, _logger);
            if (keys.ContainsKey("access_token"))
            {
                _api.AccessToken = keys["access_token"];
            }
        }

        public override Task InitializeAsync()
        {
            _spotifyAuthTimer.Change(0, 3600 * 1000);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<SpotifyTrack> GetTrackAsync(string id) => _trackProvider.GetTrackAsync(id);

        /// <inheritdoc />
        public Task<IEnumerable<SpotifyTrack>> SearchAsync(
            string query,
            SearchType searchType = SearchType.Track,
            int maxResults = 0) => _searchProvider.SearchAsync(query, searchType, maxResults);

        /// <inheritdoc />
        public Task<SpotifyCollection> GetPlaylistAsync(
            string id,
            int startIndex = 0,
            int maxResults = 0) => _playlistProvider.GetPlaylistAsync(id, startIndex, maxResults);

        /// <inheritdoc />
        public Task<SpotifyCollection> GetAlbumAsync(string id)
            => _albumProvider.GetAlbumAsync(id);

        /// <inheritdoc />
        public Task<(string name, Uri uri)> GetArtistAsync(string id)
            => _artistProvider.GetArtistAsync(id);
        
        /// <inheritdoc />
        public Task<IEnumerable<SpotifyTrack>> GetArtistTopTracksAsync(string id, string country = "US") 
            => _artistProvider.GetArtistTopTracksAsync(id, country);
    }
}