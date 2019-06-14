using System.Collections.Generic;
using Victoria.Entities;
using Victoria.Queue;

namespace Energize.Essentials.TrackTypes
{
    public class RadioTrack : ITrack
    {
        public static RadioTrack FromTrack(LavaTrack track)
        {
            foreach (KeyValuePair<string, string> radio in StaticData.Instance.RadioSources)
                if (radio.Value.Equals(track.Uri.AbsoluteUri))
                    return new RadioTrack(radio.Key, track);

            return new RadioTrack("unknown", track);
        }

        public RadioTrack(string genre, LavaTrack innerTrack)
        {
            this.Genre = genre;
            this.InnerTrack = innerTrack;
        }

        public string Genre { get; private set; }
        public LavaTrack InnerTrack { get; private set; }
        public string StreamURL { get => this.InnerTrack.Uri.AbsoluteUri; }
        public string Id { get => this.InnerTrack.Id; }
    }
}
