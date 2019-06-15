using System;
using System.Threading.Tasks;
using Victoria.Entities;

namespace Energize.Essentials.TrackTypes
{
    public interface ILazyLoadTrack : ITrack
    {
        Task<LavaTrack> GetInnerTrackAsync();
    }
}