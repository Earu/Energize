using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Energize.Toolkit
{
    [DataContract]
    public class Blacklist
    {
#pragma warning disable 649
        [DataMember]
        private List<ulong> IDs;
#pragma warning restore 649

        public static List<ulong> IDS;

        public static async Task Load()
        {
            string json = await File.ReadAllTextAsync("External/blacklist.json");
            Blacklist list = JsonConvert.DeserializeObject<Blacklist>(json);
            IDS = list.IDs;
        }
    }
}
