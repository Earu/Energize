using System;
using Victoria.Entities;

namespace Energize.Essentials.TrackTypes
{
    public interface ILazyLoadTrack : ITrack
    {
        LavaTrack GetInnerTrack();
    }
}