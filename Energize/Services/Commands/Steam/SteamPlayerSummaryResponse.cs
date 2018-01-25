using System.Runtime.Serialization;

namespace Energize.Services.Commands.Steam
{
    [DataContract]
    public class SteamPlayerSummaryResponse
    {
        [DataMember]
        public SteamUser[] players;
    }
}