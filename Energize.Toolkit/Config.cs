using Newtonsoft.Json;
using System.IO;

namespace Energize.Toolkit
{
    public struct DiscordConfig
    {
#if DEBUG
        [JsonProperty("TokenDev")]
        public string Token;
        [JsonProperty("BotIDDev")]
        public ulong BotID;
#else
        [JsonProperty("TokenMain")]
        public string Token;
        [JsonProperty("BotIDMain")]
        public ulong BotID;
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

    public class Config
    {
        public DiscordConfig Discord;
        public LavalinkConfig Lavalink;
        public KeysConfig Keys;
        public string TwitchURL;
        public string DBConnectionString;
        public string GitHub;

        private static Config _Instance;
        public static Config Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = Load();
                return _Instance;
            }
        }

        private static Config Load()
        {
            string json = File.ReadAllText("External/config.json");
            Config config = JsonConvert.DeserializeObject<Config>(json);

            return config;
        }
    }
}
