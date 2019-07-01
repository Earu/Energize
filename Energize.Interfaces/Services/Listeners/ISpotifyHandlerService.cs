using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using SpotifyAPI.Web.Enums;

namespace Energize.Interfaces.Services.Listeners
{
    /// <summary>
    /// Handles actions that involve the Spotify Web API
    /// </summary>
    public interface ISpotifyHandlerService : IServiceImplementation
    {
        /// <summary>
        /// Asynchronously get a Spotify track by id
        /// </summary>
        /// <param name="id">Track identifier</param>
        /// <returns>Task of Searched SpotifyTrack</returns>
        Task<SpotifyTrack> GetTrackAsync(string id);
        
        /// <summary>
        /// Asynchronously search for items on Spotify by query
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="searchType">Type of desired search results</param>
        /// <param name="maxResults">Optional: Maximum amount of track results</param>
        /// <returns>Task of collection of the searched SpotifyTracks</returns>
        Task<IEnumerable<SpotifyTrack>> SearchAsync(
            string query, 
            SearchType searchType = SearchType.Track,
            int maxResults = 0);

        /// <summary>
        /// Asynchronously get a playlist and its items by id
        /// </summary>
        /// <param name="id">Playlist identifier</param>
        /// <param name="startIndex">Optional: Offset in tracks collection (start from index)</param>
        /// <param name="maxResults">Optional: Maximum amount of track results</param>
        /// <returns>Task of the playlist converted to a SpotifyCollection</returns>
        Task<SpotifyCollection> GetPlaylistAsync(
            string id,
            int startIndex = 0,
            int maxResults = 0);
        
        /// <summary>
        /// Asynchronously get an album and its items by id
        /// </summary>
        /// <param name="id">Album identifier</param>
        /// <returns>Task of the album converted to a SpotifyCollection</returns>
        Task<SpotifyCollection> GetAlbumAsync(string id);

        /// <summary>
        /// Asynchronously get an artist's name and Uri by id
        /// </summary>
        /// <param name="id">Artist identifier</param>
        /// <returns>Task of tuple / pair of name and Uri</returns>
        Task<(string name, Uri uri)> GetArtistAsync(string id);
        
        /// <summary>
        /// Asynchronously get top tracks by an artist by id
        /// </summary>
        /// <param name="id">Artist identifier</param>
        /// <param name="country">Optional: which country to filter the top tracks from</param>
        /// <returns></returns>
        Task<IEnumerable<SpotifyTrack>> GetArtistTopTracksAsync(string id, string country = "US");
    }
}