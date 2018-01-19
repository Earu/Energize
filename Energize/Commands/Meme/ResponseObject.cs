using System.Runtime.Serialization;

namespace Energize.Commands.Meme
{
    [DataContract]
    public class ResponseObject
    {
        [DataMember]
        public bool success;

        [DataMember]
        public DataObject data;
    }
}
