using NexusForever.WorldServer.Game.CSI;
using NexusForever.WorldServer.Game.Entity;
using System;

namespace NexusForever.WorldServer.Game.Spell
{
    public class SpellParameters
    {
        public SpellInfo SpellInfo { get; set; }
        public SpellInfo ParentSpellInfo { get; set; }
        public SpellInfo RootSpellInfo { get; set; }
        public bool UserInitiatedSpellCast { get; set; } = true;
        public uint PrimaryTargetId { get; set; }
        public Position Position { get; set; }
        public ushort TaxiNode { get; set; }
        public ClientSideInteraction ClientSideInteraction { get; set; }
        public uint CastTimeOverride { get; set; }
        public uint ThresholdValue { get; set; }
        public bool IsProxy { get; set; }
        public Action OnExecuteComplete { get; set; }
    }
}
