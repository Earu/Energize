using System.Runtime.Serialization;

namespace Energize.Services.Commands.Meme
{
    [DataContract]
    public class DataObject
    {
        [DataMember]
        public MemeObject[] memes;
    }
}
