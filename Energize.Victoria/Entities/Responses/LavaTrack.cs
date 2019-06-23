using Newtonsoft.Json;
using System;
using Victoria.Queue;

namespace Victoria.Entities
{
    public class LavaTrack : ILavaTrack
    {
        [JsonIgnore]
        public string Hash { get; set; }

        [JsonProperty("identifier")]
        public virtual string Id { get; internal set; }

        [JsonProperty("isSeekable")]
        public virtual bool IsSeekable { get; internal set; }

        [JsonProperty("author")]
        public virtual string Author { get; internal set; }

        [JsonProperty("isStream")]
        public virtual bool IsStream { get; internal set; }

        [JsonIgnore]
        public virtual TimeSpan Position
        {
            get => new TimeSpan(this.TrackPosition);
            set =>
                this.TrackPosition = value.Ticks;
        }

        [JsonProperty("position")]
        internal long TrackPosition { get; set; }

        [JsonIgnore]
        public virtual TimeSpan Length
        {
            get => TimeSpan.FromMilliseconds(this.TrackLength);
            set => this.TrackLength = value.Milliseconds;
        }

        [JsonProperty("length")]
        internal long TrackLength { get; set; }

        [JsonProperty("title")]
        public virtual string Title { get; internal set; }

        [JsonProperty("uri")]
        public virtual Uri Uri { get; internal set; }

        [JsonIgnore]
        public virtual string Provider
        {
            get => this.Uri.GetProvider();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void ResetPosition()
        {
            this.Position = TimeSpan.Zero;
        }
    }
}
