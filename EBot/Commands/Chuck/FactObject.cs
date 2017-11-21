using System.Runtime.Serialization;

namespace EBot.Commands.Chuck
{
    [DataContract]
    public class FactObject
    {
        [DataMember]
        public string value;
    }
}
