using System.Runtime.Serialization;

namespace Energize.Services.Commands.Chuck
{
    [DataContract]
    public class FactObject
    {
        [DataMember]
        public string value;
    }
}
