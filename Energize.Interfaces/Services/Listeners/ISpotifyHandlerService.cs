using System.Collections.Generic;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using SpotifyAPI.Web.Enums;

namespace Energize.Interfaces.Services.Listeners
{
    public interface ISpotifyHandlerService
    {
        Task<IEnumerable<SpotifyTrack>> SearchAsync(string query, SearchType searchType);

        Task<SpotifyTrack> GetTrackAsync(string id);
    }
}