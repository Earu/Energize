using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Energize.Essentials
{
    public class StaticData
    {
        public Dictionary<string, string[]> SocialActions { get; set; }
        public List<string> Tips { get; set; }
        public Dictionary<string, string> RadioSources { get; set; }

        public string AsciiArt = @" ______ _   _ ______ _____   _____ _____ ____________
|  ____| \ | |  ____|  __ \ / ____|_   _|___  /  ____|
| |__  |  \| | |__  | |__) | |  __  | |    / /| |__
|  __| | . ` |  __| |  _  /| | |_ | | |   / / |  __|
| |____| |\  | |____| | \ \| |__| |_| |_ / /__| |____
|______|_| \_|______|_|  \_\\_____|_____/_____|______|";

        public static StaticData Instance { get; } = Load();

        private static StaticData Load()
        {
            string json = File.ReadAllText("Data/static.json");
            return JsonConvert.DeserializeObject<StaticData>(json);
        }
    }
}
