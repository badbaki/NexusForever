using NexusForever.Shared.GameTable;
using NexusForever.Shared.GameTable.Model;
using NexusForever.WorldServer.Game.CSI.Static;
using NexusForever.WorldServer.Game.Entity;
using NexusForever.WorldServer.Game.Spell;
using NLog;
using System;

namespace NexusForever.WorldServer.Game.CSI
{
    public class ClientSideInteraction
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        public uint ClientUniqueId { get; private set; } = 0;
        public WorldEntity ActivateUnit { get; private set; } = null;
        public CSIType CsiType { get; private set; } = CSIType.Interaction;
        public ClientSideInteractionEntry Entry { get; private set; }

        private Player Owner { get; set; } = null;

        public ClientSideInteraction(Player owner, WorldEntity activateUnit, uint clientUniqueId)
        {
            Owner = owner;
            ActivateUnit = activateUnit;
            ClientUniqueId = clientUniqueId;
        }

        public void TriggerReady()
        {
            // This should be used for things like responding after a timer, or ensuring an event happens during an interaction's cast time.
        }

        public void TriggerFail()
        {
            ActivateUnit.OnActivateFail(Owner);
        }

        public void TriggerSuccess()
        {
            ActivateUnit.OnActivateSuccess(Owner);
        }

        public void SetClientSideInteractionEntry(ClientSideInteractionEntry clientSideInteractionEntry)
        {
            if (clientSideInteractionEntry == null)
            {
                CsiType = CSIType.Interaction;
                return;
            }

            Entry = clientSideInteractionEntry;
            CsiType = (CSIType)Entry.InteractionType;
        }
    }
}