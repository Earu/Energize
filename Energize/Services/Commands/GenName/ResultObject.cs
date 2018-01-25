using System.Runtime.Serialization;

namespace Energize.Services.Commands.GenName
{
    [DataContract]
    public class ResultObject
    {
        [DataMember]
        public DataObject[] results;
    }
}
