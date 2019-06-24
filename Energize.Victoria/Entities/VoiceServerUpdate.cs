using Discord.WebSocket;
using Newtonsoft.Json;

namespace Victoria.Entities
{
    internal sealed class VoiceServerUpdate
    {
        [JsonProperty("token")]
        public string Token { get; }

        [JsonProperty("guildid")]
        public string GuildId { get; }

        [JsonProperty("endpoint")]
        public string Endpoint { get; }

        public VoiceServerUpdate(SocketVoiceServer server)
        {
            this.Token = server.Token;
            this.Endpoint = server.Endpoint;
            this.GuildId = $"{server.Guild.Id}";
        }
    }
}