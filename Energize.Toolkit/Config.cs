using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Energize.Toolkit
{
    [DataContract]
    public class Config
    {
#pragma warning disable 649
        [DataMember]
        private string TokenDev;
        [DataMember]
        private string TokenMain;
        [DataMember]
        private string TwitchURL;
        [DataMember]
        private ulong OwnerID;
        [DataMember]
        private ulong BotIDMain;
        [DataMember]
        private ulong BotIDDev;
        [DataMember]
        private string GoogleAPIKey;
        [DataMember]
        private string MashapeKey;
        [DataMember]
        private ulong FeedbackChannelID;
        [DataMember]
        private string WebSearchToken;
        [DataMember]
        private string ServerInvite;
        [DataMember]
        private string SteamAPIKey;
        [DataMember]
        private string DBConnectionString;
        [DataMember]
        private string GitHub;
        [DataMember]
        private string LVKHost;
        [DataMember]
        private int LVKPort;
        [DataMember]
        private string LVKPassword;
#pragma warning restore 649

        public static string TOKEN_MAIN;
        public static string TOKEN_DEV;
        public static string TWITCH_URL;
        public static ulong OWNER_ID;
        public static ulong BOT_ID_MAIN;
        public static ulong BOT_ID_DEV;
        public static string GOOGLE_API_KEY;
        public static string MASHAPE_KEY;
        public static ulong FEEDBACK_CHANNEL_ID;
        public static string WEB_SEARCH_TOKEN;
        public static string SERVER_INVITE;
        public static string STEAM_API_KEY;
        public static string DB_CONNECTION_STRING;
        public static string GITHUB;
        public static string LVK_HOST;
        public static int LVK_PORT;
        public static string LVK_PASSWORD;

        public static async Task Load()
        {
            string json = await File.ReadAllTextAsync("External/config.json");
            Config config = JsonConvert.DeserializeObject<Config>(json);

            TOKEN_DEV = config.TokenDev;
            TOKEN_MAIN = config.TokenMain;
            TWITCH_URL = config.TwitchURL;
            BOT_ID_MAIN = config.BotIDMain;
            BOT_ID_DEV = config.BotIDDev;
            OWNER_ID = config.OwnerID;
            GOOGLE_API_KEY = config.GoogleAPIKey;
            MASHAPE_KEY = config.MashapeKey;
            FEEDBACK_CHANNEL_ID = config.FeedbackChannelID;
            WEB_SEARCH_TOKEN = config.WebSearchToken;
            SERVER_INVITE = config.ServerInvite;
            STEAM_API_KEY = config.SteamAPIKey;
            DB_CONNECTION_STRING = config.DBConnectionString;
            GITHUB = config.GitHub;
            LVK_HOST = config.LVKHost;
            LVK_PORT = config.LVKPort;
            LVK_PASSWORD = config.LVKPassword;
        }
    }
}
