using System.Collections.Generic;
using NexusForever.Shared.Configuration;

namespace NexusForever.WorldServer
{
    public class WorldServerConfiguration
    {
        public struct MapConfig
        {
            public string MapPath { get; set; }
            public List<ushort> PrecacheBaseMaps { get; set; }
            public List<ushort> PrecacheMapSpawns { get; set; }
            public uint? GridActionThreshold { get; set; }
            public double? GridUnloadTimer { get; set; }
        }

        public struct StoreConfig
        {
            public float ForcedProtobucksPrice { get; set; }
            public float ForcedOmnibitsPrice { get; set; }
            public bool CurrencyProtobucksEnabled { get; set; }
            public bool CurrencyOmnibitsEnabled { get; set; }
        }

        public class RulesConfig
        {
            public bool CrossFactionChat { get; set; } = true;
        }

        public NetworkConfig Network { get; set; }
        public DatabaseConfig Database { get; set; }
        public MapConfig Map { get; set; }
        public StoreConfig Store { get; set; }
        public bool UseCache { get; set; } = false;
        public ushort RealmId { get; set; }
        public uint LengthOfInGameDay { get; set; }
        public RulesConfig Rules { get; set; } = new RulesConfig();
        public ulong DefaultRole { get; set; } = 1;
        public string MessageOfTheDay { get; set; } = "";
    }
}
