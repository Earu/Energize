using Victoria.Entities;
using Victoria.Queue;

namespace Energize.Essentials.TrackTypes
{
    public class RadioTrack : IQueueObject
    {
        public RadioTrack(ILavaTrack innerTrack)
        {
            foreach ((string key, string value) in StaticData.Instance.RadioSources)
                if (value.Equals(innerTrack.Uri.AbsoluteUri))
                    this.Genre = key;

            this.Genre = "unknown";
            this.InnerTrack = innerTrack;
        }

        public RadioTrack(string genre, ILavaTrack innerTrack)
        {
            this.Genre = genre;
            this.InnerTrack = innerTrack;
        }

        public string Genre { get; }
        public ILavaTrack InnerTrack { get; }
        public string StreamURL => this.InnerTrack.Uri.AbsoluteUri;
        public string Id => this.InnerTrack.Id;
    }
}
