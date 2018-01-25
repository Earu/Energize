using System.Runtime.Serialization;

namespace Energize.Services.Commands.GenName
{
    [DataContract]
    public class LoginObject
    {
        [DataMember]
        public string username;
    }
}
