using System.Runtime.Serialization;

namespace Energize.Commands.Meme
{
    [DataContract]
    public class DataObject
    {
        [DataMember]
        public MemeObject[] memes;
    }
}
