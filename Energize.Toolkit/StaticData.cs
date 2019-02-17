using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Energize.Toolkit
{
    [DataContract]
    public class StaticData
    {
#pragma warning disable 649
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
        public string[] AnimeEmotes;
        [DataMember]
        public string[][] AnimeDecorations;
        [DataMember]
        public string[] HentaiQuotes;
        [DataMember]
        public char[] Zalgo;

#pragma warning restore 649

        public static string[] ADJECTIVES;
        public static string[] NOUNS;
        public static string[] VOWELS;
        public static string[] EIGHT_BALL_ANSWERS;
        public static string[] PICK_ANSWERS;
        public static string[] DRAMA_FILTER;
        public static string[] APOLOGIZE_FILTER;
        public static string[] ANIME_EMOTES;
        public static string[][] ANIME_DECORATIONS;
        public static string[] HENTAI_QUOTES;
        public static char[] ZALGO;

        public static async Task Load()
        {
            using (StreamReader reader = File.OpenText("External/data.json"))
            {
                string json = await reader.ReadToEndAsync();
                StaticData data = JsonConvert.DeserializeObject<StaticData>(json);
                ADJECTIVES = data.Adjectives;
                NOUNS = data.Nouns;
                VOWELS = data.Vowels;
                EIGHT_BALL_ANSWERS = data.EightBallAnswers;
                PICK_ANSWERS = data.PickAnswers;
                DRAMA_FILTER = data.DramaFilter;
                APOLOGIZE_FILTER = data.ApologizeFilter;
                ANIME_EMOTES = data.AnimeEmotes;
                ANIME_DECORATIONS = data.AnimeDecorations;
                HENTAI_QUOTES = data.HentaiQuotes;
                ZALGO = data.Zalgo;
            }
        }
    }
}
