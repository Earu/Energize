using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Energize.Essentials
{
    public class Blacklist
    {
        public List<ulong> IDs;

        public static Blacklist Instance { get; } = Load();

        private static Blacklist Load()
        {
            string json = File.ReadAllText("Settings/blacklist.json");
            Blacklist list = JsonConvert.DeserializeObject<Blacklist>(json);

            return list;
        }
    }
}
