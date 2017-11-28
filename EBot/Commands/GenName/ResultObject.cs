using System.Runtime.Serialization;

namespace EBot.Commands.GenName
{
    [DataContract]
    public class ResultObject
    {
        [DataMember]
        public DataObject[] results;
    }
}
