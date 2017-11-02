using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace EBot
{
    [DataContract]
    public class EBotCredentials
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

        public static string TOKEN_MAIN;
        public static string TOKEN_DEV;
        public static string TWITCH_URL;
        public static ulong OWNER_ID;
        public static ulong BOT_ID_MAIN;
        public static ulong BOT_ID_DEV;
        public static string GOOGLE_API_KEY;

        public static async Task Load()
        {
            using (StreamReader reader = File.OpenText("External/credentials.json"))
            {
                string json = await reader.ReadToEndAsync();
                EBotCredentials credentials = JsonConvert.DeserializeObject<EBotCredentials>(json);

                TOKEN_DEV = credentials.TokenDev;
                TOKEN_MAIN = credentials.TokenMain;
                TWITCH_URL = credentials.TwitchURL;
                BOT_ID_MAIN = credentials.BotIDMain;
                BOT_ID_DEV = credentials.BotIDDev;
                OWNER_ID = credentials.OwnerID;
                GOOGLE_API_KEY = credentials.GoogleAPIKey;
            }
        }
    }
}
