using Newtonsoft.Json;
using System;

namespace Victoria.Entities
{
    internal struct PlayerState
    {
        [JsonIgnore]
        public DateTimeOffset Time
            => DateTimeOffset.FromUnixTimeMilliseconds(this.LongTime);

        [JsonProperty("time")]
        private long LongTime { get; set; }

        [JsonIgnore]
        public TimeSpan Position
            => TimeSpan.FromMilliseconds(this.LongPosition);

        [JsonProperty("position")]
        private long LongPosition { get; set; }
    }
}