using System.Runtime.Serialization;

namespace EBot.Commands.Steam
{
    [DataContract]
    public class SteamPlayerSummaryResponse
    {
        [DataMember]
        public SteamUser[] players;
    }
}