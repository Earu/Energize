using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Energize.Services.Database.Models
{
    public class DiscordChannel
    {
        [Key]
        public int Identity { get; set; }
        public ulong ID { get; set; }

        public DiscordChannel() { }

        public DiscordChannel(ulong id)
        {
            this.ID = id;
        }
    }
}
