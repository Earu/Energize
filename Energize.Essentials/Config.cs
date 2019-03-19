using Newtonsoft.Json;
using System.IO;

namespace Energize.Essentials
{
    public struct DiscordConfig
    {
#if DEBUG
        [JsonProperty("TokenDev")]
        public string Token;
        [JsonProperty("BotIDDev")]
        public ulong BotID;
        [JsonProperty("PrefixDev")]
        public string Prefix;
#else
        [JsonProperty("TokenProd")]
        public string Token;
        [JsonProperty("BotIDProd")]
        public ulong BotID;
        [JsonProperty("PrefixProd")]
        public string Prefix;
#endif

        public string ServerInvite;
        public ulong OwnerID;
        public ulong FeedbackChannelID;
    }

    public struct LavalinkConfig
    {
        public string Host;
        public int Port;
        public string Password;
    }

    public struct KeysConfig
    {
        public string GoogleAPIKey;
        public string MashapeKey;
        public string WebSearchToken;
        public string SteamAPIKey;
    }

    public struct URIConfig
    {
        public string MarkovDirectory;
        public string LuaDirectory;
        public string TwitchURL;
        public string GitHubURL;
    }

    public class Config
    {
        public DiscordConfig Discord;
        public LavalinkConfig Lavalink;
        public KeysConfig Keys;
        public URIConfig URIs;
        public string DBConnectionString;

        public static Config Instance { get; } = Load();

        private static Config Load()
        {
            string json = File.ReadAllText("Settings/config.json");
            Config config = JsonConvert.DeserializeObject<Config>(json);

            return config;
        }
    }
}
