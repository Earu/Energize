using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Energize.Services.Commands.Steam
{
    [DataContract]
    public class SteamVanityResponse
    {
        [DataMember]
        public string steamid;

        [DataMember]
        public int success;
    }
}