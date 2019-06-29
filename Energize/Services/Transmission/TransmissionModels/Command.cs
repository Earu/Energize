using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Energize.Services.Transmission.TransmissionModels
{
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
        public IEnumerable<Commands.Command.CommandCondition> Conditions { get; set; }

        public static Command ToModel(Commands.Command.Command cmd)
        {
            return new Command
            {
                Name = cmd.name,
                Usage = cmd.usage,
                Help = cmd.help,
                ModuleName = cmd.moduleName,
                Parameters = cmd.parameters,
                Permissions = cmd.permissions.Select(perm => perm.ToString()),
                Conditions = cmd.conditions,
            };
        }
    }
}
