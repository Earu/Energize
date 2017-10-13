using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace EBot.Commands.Warframe
{
    [DataContract]
    public class WReward
    {
        [DataMember]
        public int credits;
        [DataMember]
        public string[] items;
    }
}
