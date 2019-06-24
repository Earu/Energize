using Newtonsoft.Json;

namespace Victoria.Entities.Payloads
{
    internal sealed class PausePayload : LavaPayload
    {
        [JsonProperty("pause")]
        public bool Pause { get; set; }

        public PausePayload(ulong guildId, bool pause) : base(guildId, "pause")
        {
            this.Pause = pause;
        }
    }
}
