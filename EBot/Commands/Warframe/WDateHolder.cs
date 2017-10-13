using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace EBot.Commands.Warframe
{
    [DataContract]
    public class WDateHolder
    {
        [DataMember]
        [JsonProperty("$date")]
        public WDate date;
    }
}