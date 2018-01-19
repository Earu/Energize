using System.Runtime.Serialization;

namespace Energize.Commands.Chuck
{
    [DataContract]
    public class FactObject
    {
        [DataMember]
        public string value;
    }
}
