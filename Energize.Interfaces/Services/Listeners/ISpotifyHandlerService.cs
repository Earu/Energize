using System.Collections.Generic;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using SpotifyAPI.Web.Enums;

namespace Energize.Interfaces.Services.Listeners
{
    public interface ISpotifyHandlerService : IServiceImplementation
    {
        Task<SpotifyTrack> GetTrackAsync(string id);
        
        Task<IEnumerable<SpotifyTrack>> SearchAsync(string query, SearchType searchType = SearchType.All);
    }
}