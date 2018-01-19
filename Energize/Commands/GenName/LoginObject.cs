using System.Runtime.Serialization;

namespace Energize.Commands.GenName
{
    [DataContract]
    public class LoginObject
    {
        [DataMember]
        public string username;
    }
}
