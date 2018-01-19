using System.Runtime.Serialization;

namespace Energize.Commands.GenName
{
    [DataContract]
    public class ResultObject
    {
        [DataMember]
        public DataObject[] results;
    }
}
