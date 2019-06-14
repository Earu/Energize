using System;
using Victoria.Entities;
using Victoria.Queue;

namespace Energize.Essentials.TrackTypes
{
    public interface ITrack : IQueueObject
    {
        LavaTrack InnerTrack { get; }
    }
}
