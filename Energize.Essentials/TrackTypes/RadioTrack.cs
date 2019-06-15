using System.Collections.Generic;
using Victoria.Entities;

namespace Energize.Essentials.TrackTypes
{
    public class RadioTrack : DependentTrack
    {
        public RadioTrack(ITrack track)
        {
            foreach ((string key, string value) in StaticData.Instance.RadioSources)
                if (value.Equals(track.Uri.AbsoluteUri))
                    Genre = key;

            Genre = "unknown";
            InnerTrack = track.InnerTrack;
        }

        public RadioTrack(string genre, LavaTrack innerTrack)
        {
            Genre = genre;
            InnerTrack = innerTrack;
        }

        public string Genre { get; }
        public LavaTrack InnerTrack { get; }
        public string StreamURL { get => InnerTrack.Uri.AbsoluteUri; }
        public string Id { get => InnerTrack.Id; }
    }
}
