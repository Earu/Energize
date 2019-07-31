using Newtonsoft.Json;

namespace Energize.Services.Transmission.TransmissionModels
{
    public class DiscordBotsVote
    {
        [JsonProperty("bot")]
        public ulong BotId;

        [JsonProperty("user")]
        public ulong UserId;

        [JsonProperty("isWeekend")]
        public bool IsWeekend;
    }
}
