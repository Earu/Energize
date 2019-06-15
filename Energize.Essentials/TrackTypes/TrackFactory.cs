using System;
using Victoria.Entities;

namespace Energize.Essentials.TrackTypes
{
    public static class TrackFactory
    {
        public static ITrack Create(LavaTrack innerTrack, string id = null) => new DependentTrack(innerTrack, id);
    }
}
