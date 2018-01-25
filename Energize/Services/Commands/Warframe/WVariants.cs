using System.Runtime.Serialization;

namespace Energize.Services.Commands.Warframe
{
    [DataContract]
    public class WVariants
    {
        [DataMember]
        public string missionType;
        [DataMember]
        public string modifierType;
        [DataMember]
        public string node;
        [DataMember]
        public string tileset;
    }
}