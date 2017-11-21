using System.Runtime.Serialization;

namespace EBot.Commands.Meme
{
    [DataContract]
    public class MemeObject
    {
        [DataMember]
        public string url;
    }
}
