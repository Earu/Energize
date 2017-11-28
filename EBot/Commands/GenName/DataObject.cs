using System.Runtime.Serialization;

namespace EBot.Commands.GenName
{
    [DataContract]
    public class DataObject
    {
        [DataMember]
        public LoginObject login;
    }
}
