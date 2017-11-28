using System.Runtime.Serialization;

namespace EBot.Commands.GenName
{
    [DataContract]
    public class LoginObject
    {
        [DataMember]
        public string username;
    }
}
