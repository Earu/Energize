using System.Runtime.Serialization;

namespace Energize.Commands.Steam
{
    [DataContract]
    public class SteamPlayerSummaryResponse
    {
        [DataMember]
        public SteamUser[] players;
    }
}