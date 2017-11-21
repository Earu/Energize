using System.Runtime.Serialization;

namespace EBot.Commands.Meme
{
    [DataContract]
    public class DataObject
    {
        [DataMember]
        public MemeObject[] memes;
    }
}
