using Energize.Interfaces.DatabaseModels;
using System.ComponentModel.DataAnnotations;

namespace Energize.Services.Database.Models
{
    public class DiscordGuild : IDiscordGuild
    {
        [Key]
        public int Identity { get; set; }
        public ulong ID { get; set; }
        public bool ShouldDeleteInvites { get; set; }
        public bool HasHallOfShames { get; set; }
        public ulong HallOfShameID { get; set; }
        public string Language { get; set; }

        public DiscordGuild() { }

        public DiscordGuild(ulong id)
        {
            this.ID = id;
            this.ShouldDeleteInvites = false;
            this.HasHallOfShames = false;
            this.HallOfShameID = 0;
            this.Language = "EN";
        }
    }
}
