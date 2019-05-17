using Newtonsoft.Json;
using System.Collections.Generic;

namespace Energize.Services.Transmission.TransmissionModels
{
    public class CommandInformation
    {
        [JsonProperty(PropertyName = "prefix")]
        public string Prefix { get; set; }

        [JsonProperty(PropertyName = "botMention")]
        public string BotMention { get; set; }

        [JsonProperty(PropertyName = "commands")]
        public List<Command> Commands { get; set; }
    }
}
