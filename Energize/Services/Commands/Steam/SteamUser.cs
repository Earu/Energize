using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Energize.Services.Commands.Steam
{
    [DataContract]
    public class SteamUser
    {
        private static string[] _States =
        {
            "Offline", //0
            "Online", //1
            "Busy", //2
            "Away", //3
            "Snooze", //4
            "Looking to trade", //5
            "Looking to play", //6
            "Invalid" //7
        };

        [DataMember]
        public long steamid;

        [DataMember]
        public string personaname;

        [DataMember]
        public string profileurl;

        [DataMember]
        public string avatarmedium;

        [DataMember]
        public string avatarfull;

        [DataMember]
        public int personastate;

        [DataMember]
        public int? gameid;

        [DataMember]
        public string gameextrainfo;

        [DataMember]
        public int timecreated;

        [DataMember]
        public int communityvisibilitystate;

        public bool IsInGame()
        {
            return this.gameid != null;
        }

        public string GetState()
        {
            int state = this.personastate;
            if(_States.Length - 1 > state && state >= 0)
            {
                if (this.IsInGame())
                {
                    return "In-Game " + this.gameextrainfo;
                }
                else
                {
                    return _States[state];
                }
            }
            else
            {
                return _States[7];
            }
        }
    }
}
