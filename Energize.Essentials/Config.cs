using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
        public ulong UpdateChannelID;
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
        public bool LazyLoad;
        public int ConcurrentPoolSize;
        public int OperationsPerThread;
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

        [YamlIgnore]
        public bool Maintenance;

#if DEBUG
        private const string ConfigPath = "Settings/config_debug.yaml";
#else
        private const string ConfigPath = "Settings/config_prod.yaml";
#endif
        public static Config Instance { get; } = Initialize();

        public async Task SaveAsync()
        {
            Serializer serializer = new Serializer();
            string yaml = serializer.Serialize(this);
            await File.WriteAllTextAsync(ConfigPath, yaml);
        }

        private static T DeserializeYaml<T>(string path)
        {
            string yaml = File.ReadAllText(path);
            Deserializer deserializer = new Deserializer();
            T obj = deserializer.Deserialize<T>(yaml);

            return obj;
        }

        private static Config Initialize()
        {
            Config config = DeserializeYaml<Config>(ConfigPath);
            config.Discord.Blacklist = LoadBlacklist();
            config.Maintenance = false;
            return config;
        }

        private static Blacklist LoadBlacklist()
            => DeserializeYaml<Blacklist>("Settings/blacklist.yaml");
    }
}
