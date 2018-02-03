using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Energize.Services.Commands.GitHub
{
    [DataContract]
    public class GitHubCommitInfo
    {
        [DataMember]
        public string message;
    }
}
