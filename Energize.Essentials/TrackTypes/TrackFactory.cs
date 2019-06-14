using System;
using Victoria.Entities;

namespace Energize.Essentials.TrackTypes
{
    public static class TrackFactory
    {
        public static ITrack Create(LavaTrack innerTrack, string id = null) => new Track(innerTrack, id);
    }
    internal class Track : ITrack
    {
        public string Id { get; }

        public LavaTrack InnerTrack { get; }

        public Track(LavaTrack innerTrack, string id = null)
        {
            InnerTrack = innerTrack;
            Id = id ?? innerTrack.Id;
        }
    }
}
