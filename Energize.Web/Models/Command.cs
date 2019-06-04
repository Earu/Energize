using Discord;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Energize.Web.Models
{
    public enum CommandCondition
    {
        AdminOnly = 0,
        NsfwOnly = 1,
        GuildOnly = 2,
        DevOnly = 3,
    }

    public class Command
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "usage")]
        public string Usage { get; set; }

        [JsonProperty(PropertyName = "help")]
        public string Help { get; set; }

        [JsonProperty(PropertyName = "moduleName")]
        public string ModuleName { get; set; }

        [JsonProperty(PropertyName = "parameters")]
        public int Parameters { get; set; }

        [JsonProperty(PropertyName = "permissions")]
        public IEnumerable<string> Permissions { get; set; }

        [JsonProperty(PropertyName = "conditions")]
        public List<CommandCondition> Conditions { get; set; }
    }
}
