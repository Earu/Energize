using System.ComponentModel.DataAnnotations;

namespace Energize.Services.Database.Models
{
    public class DiscordUser
    {
        [Key]
        public int Identity { get; set; }
        public ulong ID { get; set; }
        public ulong Level { get; set; }
        [MaxLength(30)]
        public string Style { get; set; }
        [MaxLength(300)]
        public string Description { get; set; }
        public DiscordUserStats Stats { get; set; }

        public DiscordUser() { }

        public DiscordUser(ulong id)
        {
            this.ID = id;
            this.Level = 0;
            this.Style = "none";
            this.Description = "no description provided";
            this.Stats = new DiscordUserStats();
        }
    }
}