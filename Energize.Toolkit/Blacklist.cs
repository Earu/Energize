using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Energize.Toolkit
{
    public class Blacklist
    {
        public List<ulong> IDs;

        private static Blacklist _Instance;
        public static Blacklist Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = Load();
                return _Instance;
            }
        }

        private static Blacklist Load()
        {
            string json = File.ReadAllText("External/blacklist.json");
            Blacklist list = JsonConvert.DeserializeObject<Blacklist>(json);

            return list;
        }
    }
}
