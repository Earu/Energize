using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Energize.Essentials;
using Energize.Essentials.Helpers;
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
        private readonly Logger Logger;
        private readonly SpotifyWebAPI Api;
        private readonly LavaRestClient LavaRest;
        private readonly SpotifyConfig Config;

        private readonly Timer SpotifyAuthTimer;
        private readonly SpotifyTrackProvider TrackProvider;
        private readonly SpotifySearchProvider SearchProvider;
        private readonly SpotifyPlaylistProvider PlaylistProvider;
        private readonly SpotifyAlbumProvider AlbumProvider;
        private readonly SpotifyArtistProvider ArtistProvider;


        public SpotifyHandlerService(EnergizeClient client)
        {
            this.Logger = client.Logger;
            this.Api = new SpotifyWebAPI
            {
                TokenType = "Bearer",
                UseAuth = true,
                UseAutoRetry = true
            };
            this.LavaRest = GetLavaRestClient();
            // TODO: add configuration entry
            this.Config = Essentials.Config.Instance.Spotify;
            this.SpotifyAuthTimer = new Timer(this.TradeSpotifyToken);

            SpotifyRunConfig spotifyRunConfig = new SpotifyRunConfig(this.LavaRest, this.Api, this.Config, new SpotifyTrackConverter(this.LavaRest, this.Config));
            this.TrackProvider = new SpotifyTrackProvider(spotifyRunConfig);
            this.SearchProvider = new SpotifySearchProvider(spotifyRunConfig);
            this.PlaylistProvider = new SpotifyPlaylistProvider(spotifyRunConfig);
            this.AlbumProvider = new SpotifyAlbumProvider(spotifyRunConfig);
            this.ArtistProvider = new SpotifyArtistProvider(spotifyRunConfig);
        }

        private static LavaRestClient GetLavaRestClient()
        {
            Configuration config = new Configuration
            {
                ReconnectInterval = TimeSpan.FromSeconds(15),
                ReconnectAttempts = 3,
                Host = Essentials.Config.Instance.Lavalink.Host,
                Port = Essentials.Config.Instance.Lavalink.Port,
                Password = Essentials.Config.Instance.Lavalink.Password,
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
                byte[] credBytes = Encoding.UTF8.GetBytes($"{this.Config.ClientID}:{this.Config.ClientSecret}");
                req.Headers[HttpRequestHeader.Authorization] = $"Basic {Convert.ToBase64String(credBytes)}";
                req.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            }

            string json = await HttpHelper.PostAsync("https://accounts.spotify.com/api/token?grant_type=client_credentials", string.Empty, this.Logger, null, Callback);
            if (JsonHelper.TryDeserialize(json, this.Logger, out Dictionary<string, string> keys) && keys.ContainsKey("access_token"))
                this.Api.AccessToken = keys["access_token"];
        }

        public override Task InitializeAsync()
        {
            this.SpotifyAuthTimer.Change(0, 3600 * 1000);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<SpotifyTrack> GetTrackAsync(string id) 
            => this.TrackProvider.GetTrackAsync(id);

        /// <inheritdoc />
        public Task<IEnumerable<SpotifyTrack>> SearchAsync(string query, SearchType searchType = SearchType.Track, int maxResults = 0) 
            => this.SearchProvider.SearchAsync(query, searchType, maxResults);

        /// <inheritdoc />
        public Task<SpotifyCollection> GetPlaylistAsync(string id, int startIndex = 0, int maxResults = 0) 
            => this.PlaylistProvider.GetPlaylistAsync(id, startIndex, maxResults);

        /// <inheritdoc />
        public Task<SpotifyCollection> GetAlbumAsync(string id)
            => this.AlbumProvider.GetAlbumAsync(id);

        /// <inheritdoc />
        public Task<(string name, Uri uri)> GetArtistAsync(string id)
            => this.ArtistProvider.GetArtistAsync(id);
        
        /// <inheritdoc />
        public Task<IEnumerable<SpotifyTrack>> GetArtistTopTracksAsync(string id, string country = "US") 
            => this.ArtistProvider.GetArtistTopTracksAsync(id, country);
    }
}