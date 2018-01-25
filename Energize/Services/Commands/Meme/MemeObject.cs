using System.Runtime.Serialization;

namespace Energize.Services.Commands.Meme
{
    [DataContract]
    public class MemeObject
    {
        [DataMember]
        public string url;
    }
}
