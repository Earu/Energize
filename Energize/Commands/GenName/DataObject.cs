using System.Runtime.Serialization;

namespace Energize.Commands.GenName
{
    [DataContract]
    public class DataObject
    {
        [DataMember]
        public LoginObject login;
    }
}
