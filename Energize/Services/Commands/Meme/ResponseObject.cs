using System.Runtime.Serialization;

namespace Energize.Services.Commands.Meme
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
