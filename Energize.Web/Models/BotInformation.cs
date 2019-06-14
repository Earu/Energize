using Newtonsoft.Json;

namespace Energize.Web.Models
{
    public class BotInformation
    {
        [JsonProperty(PropertyName = "userCount")]
        public int UserCount { get; set; }

        [JsonProperty(PropertyName = "serverCount")]
        public int ServerCount { get; set; }
    }
}
