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

        public static string TOKEN_MAIN;
        public static string TOKEN_DEV;
        public static string TWITCH_URL;

        public static async Task Load()
        {
            using (StreamReader reader = File.OpenText("credentials.json"))
            {
                string json = await reader.ReadToEndAsync();
                EBotCredentials credentials = JsonConvert.DeserializeObject<EBotCredentials>(json);

                TOKEN_DEV = credentials.TokenDev;
                TOKEN_MAIN = credentials.TokenMain;
                TWITCH_URL = credentials.TwitchURL;
            }
        }
    }
}
