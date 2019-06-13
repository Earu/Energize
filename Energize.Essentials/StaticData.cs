using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Energize.Essentials
{
    public class StaticData
    {
        public Dictionary<string, string[]> SocialActions;
        public List<string> Tips;
        
        public string AsciiArt = @" ______ _   _ ______ _____   _____ _____ ____________
|  ____| \ | |  ____|  __ \ / ____|_   _|___  /  ____|
| |__  |  \| | |__  | |__) | |  __  | |    / /| |__
|  __| | . ` |  __| |  _  /| | |_ | | |   / / |  __|
| |____| |\  | |____| | \ \| |__| |_| |_ / /__| |____
|______|_| \_|______|_|  \_\\_____|_____/_____|______|";

        public Dictionary<string, string> RadioSources = new Dictionary<string, string>
        {
            ["anime"] = "https://listen.moe/opus",
            ["kpop"] = "https://listen.moe/kpop/opus",
            ["drum n bass"] = "http://bassdrive.radioca.st/;stream/1",
            ["metal"] = "http://ice1.somafm.com/metal-128-mp3",
            ["dubstep"] = "http://ice1.somafm.com/dubstep-128-mp3",
            ["70s"] = "http://ice1.somafm.com/seventies-128-mp3",
            ["alt rock"] = "http://ice1.somafm.com/bagel-128-mp3",
            ["jazz"] = "http://ice1.somafm.com/sonicuniverse-128-mp3",
            ["defcon"] = "http://ice1.somafm.com/defcon-128-mp3",
            ["progressive house"] = "http://ice1.somafm.com/thetrip-128-mp3",
            ["folk"] = "http://ice1.somafm.com/folkfwd-128-mp3",
            ["celtic"] = "http://ice1.somafm.com/thistle-128-mp3",
            ["deep house"] = "http://ice1.somafm.com/beatblender-128-mp3",
        };

        public static StaticData Instance { get; } = Load();

        private static StaticData Load()
        {
            string json = File.ReadAllText("Data/static.json");
            return JsonConvert.DeserializeObject<StaticData>(json);
        }
    }
}
