using Newtonsoft.Json;
using System.Collections.Generic;

namespace Energize.Web.Models
{
    public class CommandInformation
    {
        public CommandInformation()
        {
            this.Prefix = string.Empty;
            this.BotMention = string.Empty;
            this.Commands = new List<Command>();
        }

        [JsonProperty(PropertyName = "prefix")]
        public string Prefix { get; set; }

        [JsonProperty(PropertyName = "botMention")]
        public string BotMention { get; set; }

        [JsonProperty(PropertyName = "commands")]
        public List<Command> Commands { get; set; }
    }
}
