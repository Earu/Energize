using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace Energize.Essentials
{
    public struct DiscordConfig
    {
        [YamlMember(Alias = "TokenDev")]
        public string TokenDev { get; set; }
        [YamlMember(Alias = "BotIDDev")]
        public ulong BotIDDev { get; set; }
        [YamlMember(Alias = "PrefixDev")]
        public string PrefixDev { get; set; }
        [YamlMember(Alias = "TokenProd")]
        public string TokenProd { get; set; }
        [YamlMember(Alias = "BotIDProd")]
        public ulong BotIDProd { get; set; }
        [YamlMember(Alias = "PrefixProd")]
        public string PrefixProd { get; set; }

#if DEBUG
        public string Token { get => this.TokenDev; }
        public ulong BotID { get => this.BotIDDev; }
        public string Prefix { get => this.PrefixDev; }
#else
        public string Token { get => this.TokenProd; }
        public ulong BotID { get => this.BotIDProd; }
        public string Prefix { get => this.PrefixProd; }
#endif
        public char Separator;
        public ulong OwnerID;
        public ulong FeedbackChannelID;
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

    public struct KeysConfig
    {
        public string TwitchKey;
    }

    public struct URIConfig
    {
        public string TwitchURL;
        public string GitHubURL;
        public string InviteURL;
        public string WebsiteURL;
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
            => DeserializeYAML<Config>("Settings/config.yaml");

        private static Blacklist LoadBlacklist()
            => DeserializeYAML<Blacklist>("Settings/blacklist.yaml");
    }
}
