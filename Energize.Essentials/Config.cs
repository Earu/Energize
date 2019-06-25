using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace Energize.Essentials
{
    public struct DiscordConfig
    {
        public string Token;
        public ulong BotID;
        public string Prefix;
        public char Separator;
        public ulong OwnerID;
        public ulong FeedbackChannelID;
        public ulong BugReportChannelID;
        public string BotListToken;
        public string BotsToken;

        [YamlIgnore]
        public Blacklist Blacklist { get; set; }
    }

    public struct LavalinkConfig
    {
        public string Host;
        public int Port;
        public string Password;
    }

    public struct OctovisorConfig
    {
        public string Address;
        public int Port;
        public string Token;
        public string ProcessName;
    }

    public struct SpotifyConfig
    {
        public string ClientID;
        public string ClientSecret;
    }

    public struct KeysConfig
    {
        public string TwitchKey;
        public string YoutubeKey;
    }

    public struct URIConfig
    {
        public string TwitchURL;
        public string GitHubURL;
        public string InviteURL;
        public string WebsiteURL;
        public string DiscordURL;
    }

    public struct Blacklist
    {
        public List<ulong> IDs;
    }

    public class Config
    {
        public DiscordConfig Discord;
        public LavalinkConfig Lavalink;
        public OctovisorConfig Octovisor;
        public SpotifyConfig Spotify;
        public KeysConfig Keys;
        public URIConfig URIs;
        public string DBConnectionString;

        public static Config Instance { get; } = Initialize();

        private static T DeserializeYAML<T>(string path)
        {
            string yaml = File.ReadAllText(path);
            Deserializer deserializer = new Deserializer();
            T obj = deserializer.Deserialize<T>(yaml);

            return obj;
        }

        private static Config Initialize()
        {
            Config config = LoadConfig();
            config.Discord.Blacklist = LoadBlacklist();
            return config;
        }

        private static Config LoadConfig()
#if DEBUG
            => DeserializeYAML<Config>("Settings/config_debug.yaml");
#else
            => DeserializeYAML<Config>("Settings/config_prod.yaml");
#endif

        private static Blacklist LoadBlacklist()
            => DeserializeYAML<Blacklist>("Settings/blacklist.yaml");
    }
}
