using System.Runtime.Serialization;

namespace Energize.Commands.Meme
{
    [DataContract]
    public class MemeObject
    {
        [DataMember]
        public string url;
    }
}
