using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Energize.Commands.Steam
{
    [DataContract]
    public class SteamVanity
    {
        [DataMember]
        public SteamVanityResponse response;
    }
}