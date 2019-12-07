using NexusForever.Shared.GameTable.Model;
using NexusForever.WorldServer.Game.Entity.Network;
using NexusForever.WorldServer.Game.Entity.Network.Model;
using NexusForever.WorldServer.Game.Entity.Static;
using NexusForever.WorldServer.Game.Map;
using System.Numerics;

namespace NexusForever.WorldServer.Game.Entity
{
    public class Plug : WorldEntity
    {
        public HousingPlotInfoEntry PlotEntry { get; }
        public HousingPlugItemEntry PlugEntry { get; }

        private Plug ReplacementPlug { get; set; }
        public ushort PlugId { get; private set; }

        public Plug()
            : base(EntityType.Plug)
        {
        }

        public Plug(HousingPlotInfoEntry plotEntry, HousingPlugItemEntry plugEntry)
            : base(EntityType.Plug)
        {
            PlotEntry = plotEntry;
            PlugEntry = plugEntry;
        }

        /*
        public override void Initialise(WorldEntityModel model)
        {
            PlugId = (ushort)model.Creature;

            base.Initialise(model);
        }
        */
        protected override IEntityModel BuildEntityModel()
        {
            return new PlugModel
            {
                SocketId  = (ushort)PlotEntry.WorldSocketId,
                PlugId    = (ushort)PlugEntry.WorldIdPlug00,
                PlugFlags = 63
            };
        }

        /// <summary>
        /// Queue a replacement <see cref="Plug"/> to assume this entity's WorldSocket and WorldPlug location
        /// </summary>
        public void EnqueueReplace(Plug newPlug)
        {
            ReplacementPlug = newPlug;
            RemoveFromMap();
        }

        public override void OnRemoveFromMap()
        {
            if (ReplacementPlug != null)
            {
                Map.EnqueueAdd(ReplacementPlug, Position);
                ReplacementPlug = null;
            }

            base.OnRemoveFromMap();
        }
    }
}
