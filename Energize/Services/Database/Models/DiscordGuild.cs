using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Energize.Services.Database.Models
{
    public class DiscordGuild
    {
        [Key]
        public int Identity { get; set; }
        public ulong ID { get; set; }
        public bool ShouldDeleteInvites { get; set; }
        [MaxLength(5)]
        public string Prefix { get; set; }
        public bool HasHallOfShames { get; set; }
        public ulong HallOfShameID { get; set; }

        public DiscordGuild() { }

        public DiscordGuild(ulong id)
        {
            this.ID = id;
            this.ShouldDeleteInvites = false;
            this.Prefix = "x";
            this.HasHallOfShames = false;
            this.HallOfShameID = 0;
        }
    }
}
