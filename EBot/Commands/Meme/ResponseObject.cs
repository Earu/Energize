using System.Runtime.Serialization;

namespace EBot.Commands.Meme
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
