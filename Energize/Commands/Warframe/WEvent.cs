using System.Runtime.Serialization;

namespace Energize.Commands.Warframe
{
    [DataContract]
    public class WEvent
    {
        [DataMember]
        public WMessage[] Messages;
        [DataMember]
        public string Prop;
        [DataMember]
        public WDateHolder Date;
        [DataMember]
        public string ImageUrl;
        [DataMember]
        public bool Priority;
        [DataMember]
        public bool MobileOnly;
    }
}