using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Energize
{
    [DataContract]
    public class EnergizeConfig
    {
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

        public static async Task Load()
        {
            using (StreamReader reader = File.OpenText("External/config.json"))
            {
                string json = await reader.ReadToEndAsync();
                EnergizeConfig config = JsonConvert.DeserializeObject<EnergizeConfig>(json);

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
            }
        }
    }
}
