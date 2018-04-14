using System.ComponentModel.DataAnnotations;

namespace Energize.Services.Database.Models
{
    public class DiscordUserStats
    {
        [Key]
        public int Identity { get; set; }
        public ulong ID { get; set; }
        public ulong SnuggledCount { get; set; }
        public ulong HuggedCount { get; set; }
        public ulong BoopedCount { get; set; }
        public ulong SlappedCount { get; set; }
        public ulong KissedCount { get; set; }
        public ulong ShotCount { get; set; }
        public ulong PetCount { get; set; }
        public ulong SpankedCount { get; set; }
        public ulong YiffedCount { get; set; }
        public ulong NomedCount { get; set; }
        public ulong LickedCount { get; set; }
        public ulong BittenCount { get; set; }

        public DiscordUserStats() { }

        public DiscordUserStats(ulong id)
        {
            this.ID = id;
            this.SnuggledCount = 0;
            this.HuggedCount = 0;
            this.BoopedCount = 0;
            this.SlappedCount = 0;
            this.KissedCount = 0;
            this.ShotCount = 0;
            this.PetCount = 0;
            this.SpankedCount = 0;
            this.YiffedCount = 0;
            this.NomedCount = 0;
            this.LickedCount = 0;
            this.BittenCount = 0;
        }
    }
}
