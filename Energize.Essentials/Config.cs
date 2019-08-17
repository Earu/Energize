using Energize.Essentials.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Energize.Essentials
{
    public class DiscordConfig
    {
        public string Token { get; set; }
        public ulong BotID { get; set; }
        public string Prefix { get; set; }
        public char Separator { get; set; }
        public ulong OwnerID { get; set; }
        public ulong FeedbackChannelID { get; set; }
        public ulong BugReportChannelID { get; set; }
        public ulong UpdateChannelID { get; set; }
        public string BotListToken { get; set; }
        public string BotsToken { get; set; }

        [YamlIgnore]
        public Blacklist Blacklist { get; set; }
    }

    public class LavalinkConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
    }

    public class OctovisorConfig
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public string Token { get; set; }
        public string ProcessName { get; set; }
    }

    public class SpotifyConfig
    {
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public bool LazyLoad { get; set; }
        public int ConcurrentPoolSize { get; set; }
        public int OperationsPerThread { get; set; }
    }

    public class MailConfig
    {
        public string ServerAddress { get; set; }
        public int ServerPort { get; set; }
        public string MailAddress { get; set; }
        public string MailPassword { get; set; }
        public string DevMailAddress { get; set; }
    }

    public class KeysConfig
    {
        public string TwitchKey { get; set; }
        public string YoutubeKey { get; set; }
    }

    public class URIConfig
    {
        public string TwitchURL { get; set; }
        public string GitHubURL { get; set; }
        public string InviteURL { get; set; }
        public string WebsiteURL { get; set; }
        public string DiscordURL { get; set; }
    }

    public class Blacklist
    {
        public List<ulong> IDs { get; set; }
    }

    public class Config
    {
        public DiscordConfig Discord { get; set; }
        public LavalinkConfig Lavalink { get; set; }
        public OctovisorConfig Octovisor { get; set; }
        public SpotifyConfig Spotify { get; set; }
        public MailConfig Mail { get; set; }
        public KeysConfig Keys { get; set; }
        public URIConfig URIs { get; set; }
        public string DBConnectionString { get; set; }

        [YamlIgnore]
        public bool Maintenance { get; set; }

#if DEBUG
        private const string ConfigPath = "Settings/config_debug.yaml";
#else
        private const string ConfigPath = "Settings/config_prod.yaml";
#endif
        public static Config Instance { get; } = Initialize();

        public async Task SaveAsync()
        {
            if (YamlHelper.TrySerialize(this, out string yaml))
                await File.WriteAllTextAsync(ConfigPath, yaml);
        }

        private static T DeserializeYaml<T>(string path)
        {
            if (File.Exists(path))
            {
                string yaml = File.ReadAllText(path);
                if (YamlHelper.TryDeserialize(yaml, out T value))
                    return value;
            }

            throw new SerializationException($"Could not deserialize yaml file: \'{path}\'");
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
