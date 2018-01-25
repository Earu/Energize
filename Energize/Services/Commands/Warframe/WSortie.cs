using System.Runtime.Serialization;

namespace Energize.Services.Commands.Warframe
{
    [DataContract]
    public class WSortie
    {
        [DataMember]
        public WDateHolder Activation;
        [DataMember]
        public WDateHolder Expiry;
        [DataMember]
        public string Boss;
        [DataMember]
        public string Reward;
        //extradrops ?
        [DataMember]
        public int Seed;
        [DataMember]
        public WVariants[] Variants;
        [DataMember]
        public bool Twitter;
    }
}