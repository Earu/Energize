using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace EBot.Commands.Warframe
{
    [DataContract]
    public class WMission
    {
        [DataMember]
        public string missionType;
        [DataMember]
        public string faction;
        [DataMember]
        public string Location;
        [DataMember]
        public string LevelOverride;
        [DataMember]
        public string enemySpec;
        [DataMember]
        public string extraEnemySpec;
        [DataMember]
        public int minEnemyLevel;
        [DataMember]
        public int maxEnemyLevel;
        [DataMember]
        public double difficulty;
        [DataMember]
        public int seed;
        [DataMember]
        public int maxWaveNum;
        [DataMember]
        public WReward missionReward;
    }
}
