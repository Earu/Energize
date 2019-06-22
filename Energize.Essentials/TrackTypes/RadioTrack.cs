using Victoria.Entities;
using Victoria.Queue;

namespace Energize.Essentials.TrackTypes
{
    public class RadioTrack : IQueueObject
    {
        public RadioTrack(LavaTrack innerTrack)
        {
            foreach ((string key, string value) in StaticData.Instance.RadioSources)
                if (value.Equals(innerTrack.Uri.AbsoluteUri))
                    Genre = key;

            Genre = "unknown";
            InnerTrack = innerTrack;
        }

        public RadioTrack(string genre, LavaTrack innerTrack)
        {
            Genre = genre;
            InnerTrack = innerTrack;
        }

        public string Genre { get; }
        public LavaTrack InnerTrack { get; }
        public string StreamURL => InnerTrack.Uri.AbsoluteUri;
        public string Id => InnerTrack.Id;
    }
}
