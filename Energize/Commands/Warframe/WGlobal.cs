using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Energize.Commands.Warframe
{
    [DataContract]
    public class WGlobal
    {
        //public string WorldSeed;
        //public int Version;
        //public string MobileVersion;
        //public string BuildLabel;
        //public int Time;
        //public int Date;

        /*[DataMember]
        public WEvent[] Events;
        
        //public string[] Goals;*/

        [DataMember]
        public WAlert[] Alerts;

        /*
        [DataMember]
        public WSortie[] Sorties;*/
        
        //syndicate missions
        //active missions
        //globalupgrade ?
        //public WSale[] FlashSales;
        //public WInvasion[] Invasions;
        //hubevents
        //nodesoverride
        //badlandnodes
        //public WVoidTrader[] VoidTraders;

        //whatever is left

    }
}
