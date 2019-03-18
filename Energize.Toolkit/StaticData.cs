using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Energize.Toolkit
{
    public class StaticData
    {
        public string[] Adjectives;
        public string[] Nouns;
        public string[] Vowels;
        public string[] EightBallAnswers;
        public string[] PickAnswers;
        public string[] AnimeEmotes;
        public string[][] AnimeDecorations;
        public string[] HentaiQuotes;
        public char[] Zalgo;
        public Dictionary<string, string[]> SocialActions;
        
        public string AsciiArt { get; private set; }

        private static StaticData _Instance;
        public static StaticData Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = Load();
                return _Instance;
            }
        }

        private static StaticData Load()
        {
            string json = File.ReadAllText("External/config.json");
            StaticData data = JsonConvert.DeserializeObject<StaticData>(json);
            data.AsciiArt = @" ______ _   _ ______ _____   _____ _____ ____________
|  ____| \ | |  ____|  __ \ / ____|_   _|___  /  ____|
| |__  |  \| | |__  | |__) | |  __  | |    / /| |__
|  __| | . ` |  __| |  _  /| | |_ | | |   / / |  __|
| |____| |\  | |____| | \ \| |__| |_| |_ / /__| |____
|______|_| \_|______|_|  \_\\_____|_____/_____|______|";

            return data;
        }
    }
}
