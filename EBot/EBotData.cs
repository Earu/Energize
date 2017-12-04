using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace EBot
{
    [DataContract]
    class EBotData
    {
        [DataMember]
        public string[] Adjectives;
        [DataMember]
        public string[] Nouns;
        [DataMember]
        public string[] Vowels;
        [DataMember]
        public string[] EightBallAnswers;
        [DataMember]
        public string[] PickAnswers;
        [DataMember]
        public string[] DramaFilter;
        [DataMember]
        public string[] ApologizeFilter;
        [DataMember]
        public string IllegalGif;

        public static string[] ADJECTIVES;
        public static string[] NOUNS;
        public static string[] VOWELS;
        public static string[] EIGHT_BALL_ANSWERS;
        public static string[] PICK_ANSWERS;
        public static string[] DRAMA_FILTER;
        public static string[] APOLOGIZE_FILTER;
        public static string ILLEGAL_GIF;

        public static async Task Load()
        {
            using (StreamReader reader = File.OpenText("External/data.json"))
            {
                string json = await reader.ReadToEndAsync();
                EBotData data = JsonConvert.DeserializeObject<EBotData>(json);
                ADJECTIVES = data.Adjectives;
                NOUNS = data.Nouns;
                VOWELS = data.Vowels;
                EIGHT_BALL_ANSWERS = data.EightBallAnswers;
                PICK_ANSWERS = data.PickAnswers;
                DRAMA_FILTER = data.DramaFilter;
                APOLOGIZE_FILTER = data.ApologizeFilter;
                ILLEGAL_GIF = data.IllegalGif;
            }
        }
    }
}
