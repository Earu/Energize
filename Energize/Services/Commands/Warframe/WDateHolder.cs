using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Energize.Services.Commands.Warframe
{
    [DataContract]
    public class WDateHolder
    {
        [DataMember]
        [JsonProperty("$date")]
        public WDate date;
    }
}