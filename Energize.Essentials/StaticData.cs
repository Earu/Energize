using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Energize.Essentials
{
    public class StaticData
    {
        public string[] EightBallAnswers;
        public string[] PickAnswers;
        public string[] AnimeEmotes;
        public string[][] AnimeDecorations;
        public char[] Zalgo;
        public Dictionary<string, string[]> SocialActions;
        
        public string AsciiArt { get; private set; }
        public static StaticData Instance { get; } = Load();

        private static StaticData Load()
        {
            string json = File.ReadAllText("Energize/Data/static.json");
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
