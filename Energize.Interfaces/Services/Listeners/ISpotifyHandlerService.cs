using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using SpotifyAPI.Web.Enums;

namespace Energize.Interfaces.Services.Listeners
{
    public interface ISpotifyHandlerService : IServiceImplementation
    {
        Task<SpotifyTrack> GetTrackAsync(string id);
        
        Task<IEnumerable<SpotifyTrack>> SearchAsync(
            string query, 
            SearchType searchType = SearchType.All,
            int maxResults = 0);

        Task<SpotifyCollection> GetPlaylistAsync(
            string id,
            int startIndex = 0,
            int maxResults = 0);
        
        Task<SpotifyCollection> GetAlbumAsync(string id);

        Task<(string name, Uri uri)> GetArtistAsync(string id);
        
        Task<IEnumerable<SpotifyTrack>> GetArtistTopTracksAsync(string id, string country = "US");
    }
}