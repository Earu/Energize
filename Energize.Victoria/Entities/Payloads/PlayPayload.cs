using System;
using Newtonsoft.Json;

namespace Victoria.Entities.Payloads
{
    internal sealed class PlayPayload : LavaPayload
    {
        [JsonProperty("track")]
        public string Hash { get; }

        [JsonProperty("startTime")]
        public int StartTime { get; }

        [JsonProperty("endTime")]
        public int EndTime { get; }

        [JsonProperty("noReplace")]
        public bool NoReplace { get; }

        public PlayPayload(ulong guildId, string trackHash,
                              TimeSpan start, TimeSpan end,
                              bool noReplace) : base(guildId, "play")
        {
            this.Hash = trackHash;
            this.StartTime = (int)start.TotalMilliseconds;
            this.EndTime = (int)end.TotalMilliseconds;
            this.NoReplace = noReplace;
        }

        public PlayPayload(ulong guildId, string trackHash, bool noReplace) : base(guildId, "play")
        {
            this.Hash = trackHash;
            this.NoReplace = noReplace;
        }
    }
}