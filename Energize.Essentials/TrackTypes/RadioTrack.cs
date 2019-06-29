using Victoria.Entities;
using Victoria.Queue;

namespace Energize.Essentials.TrackTypes
{
    public class RadioTrack : IQueueObject
    {
        public RadioTrack(ILavaTrack innerTrack)
        {
            this.Genre = "unknown";
            this.InnerTrack = innerTrack;

            foreach ((string key, string value) in StaticData.Instance.RadioSources)
            {
                if (value.Equals(innerTrack.Uri.AbsoluteUri))
                    this.Genre = key;
            }
        }

        public string Genre { get; }
        public ILavaTrack InnerTrack { get; }
        public string StreamUrl => this.InnerTrack.Uri.AbsoluteUri;
        public string Id => this.InnerTrack.Id;
    }
}
