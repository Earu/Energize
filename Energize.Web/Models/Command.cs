using Discord;
using System.Collections.Generic;

namespace Energize.Web.Models
{
    public enum CommandCondition
    {
        AdminOnly = 0,
        NsfwOnly = 1,
        GuildOnly = 2,
        OwnerOnly = 3,
    }

    public class Command
    {
        public string Name { get; set; }
        public string Usage { get; set; }
        public string Help { get; set; }
        public string ModuleName { get; set; }
        public int Parameters { get; set; }
        public List<ChannelPermission> Permissions { get; set; }
        public List<CommandCondition> Conditions { get; set; }
    }
}
