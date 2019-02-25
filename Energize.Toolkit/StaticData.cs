using Newtonsoft.Json;
using System.Collections.Generic;
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

        [DataMember]
        public Dictionary<string, string[]> SocialActions;

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
        public static Dictionary<string, string[]> SOCIAL_ACTIONS;
        public static string ASCII_ART;

        public static async Task Load()
        {
            string json = await File.ReadAllTextAsync("External/data.json");
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
            SOCIAL_ACTIONS = data.SocialActions;
            ASCII_ART = @" ______ _   _ ______ _____   _____ _____ ____________
|  ____| \ | |  ____|  __ \ / ____|_   _|___  /  ____|
| |__  |  \| | |__  | |__) | |  __  | |    / /| |__
|  __| | . ` |  __| |  _  /| | |_ | | |   / / |  __|
| |____| |\  | |____| | \ \| |__| |_| |_ / /__| |____
|______|_| \_|______|_|  \_\\_____|_____/_____|______|";
        }
    }
}
