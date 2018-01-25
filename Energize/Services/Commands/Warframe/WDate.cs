using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Energize.Services.Commands.Warframe
{
    public class WDate
    {
        [JsonProperty("$numberLong")]
        public long numberLong;
    }
}
