using System.Runtime.Serialization;

namespace Energize.Services.Commands.GenName
{
    [DataContract]
    public class DataObject
    {
        [DataMember]
        public LoginObject login;
    }
}
