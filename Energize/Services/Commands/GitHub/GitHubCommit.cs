using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Energize.Services.Commands.GitHub
{
    [DataContract]
    public class GitHubCommit
    {
        [DataMember]
        public GitHubCommitInfo commit;
    }
}
