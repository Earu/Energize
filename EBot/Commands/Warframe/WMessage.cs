using System.Runtime.Serialization;

namespace EBot.Commands.Warframe
{
    [DataContract]
    public class WMessage
    {
        [DataMember]
        public string LanguageCode;
        [DataMember]
        public string Message;
    }
}