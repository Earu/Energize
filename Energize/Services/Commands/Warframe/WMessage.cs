using System.Runtime.Serialization;

namespace Energize.Services.Commands.Warframe
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