using NexusForever.Shared.GameTable;
using NexusForever.Shared.GameTable.Model;
using NexusForever.WorldServer.Game.CSI;
using NexusForever.WorldServer.Game.Entity.Network;
using NexusForever.WorldServer.Game.Entity.Network.Model;
using NexusForever.WorldServer.Game.Entity.Static;
using NexusForever.WorldServer.Game.Spell;
using NexusForever.WorldServer.Network.Message.Model;
using System;
using EntityModel = NexusForever.WorldServer.Database.World.Model.Entity;

namespace NexusForever.WorldServer.Game.Entity
{
    [DatabaseEntity(EntityType.CollectableUnit)]
    public class CollectableUnit : UnitEntity
    {
        public byte QuestChecklistIdx { get; private set; }

        public CollectableUnit()
            : base(EntityType.Simple)
        {
        }

        public override void Initialise(EntityModel model)
        {
            base.Initialise(model);
            QuestChecklistIdx = model.QuestChecklistIdx;
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new CollectableUnitEntityModel
            {
                CreatureId = CreatureId,
                QuestChecklistIdx = QuestChecklistIdx
            };
        }

        public override void OnActivate(Player activator)
        {
            Creature2Entry entry = GameTableManager.Instance.Creature2.GetEntry(CreatureId);
            if (entry.DatacubeId != 0u)
                activator.DatacubeManager.AddDatacube((ushort)entry.DatacubeId, int.MaxValue);
        }

        public override void OnActivateCast(Player activator, uint interactionId)
        {
            Creature2Entry entry = GameTableManager.Instance.Creature2.GetEntry(CreatureId);

            // TODO: Handle casting activate spells at correct times. Additionally, ensure Prerequisites are met to cast.
            // Creature2Entry can contain up to 4 spells to activate and prerequisite spells to trigger.
            uint spell4Id = 116;
            if (entry.Spell4IdActivate00 > 0)
                spell4Id = entry.Spell4IdActivate00;

            SpellParameters parameters = new SpellParameters
            {
                PrimaryTargetId = Guid,
                ClientSideInteraction = new ClientSideInteraction(activator, this, interactionId),
                CastTimeOverride = entry.ActivateSpellCastTime,
            };
            activator.CastSpell(spell4Id, parameters);
        }

        public override void OnActivateSuccess(Player activator)
        {
            uint progress = (uint)(1 << QuestChecklistIdx);

            Creature2Entry entry = GameTableManager.Instance.Creature2.GetEntry(CreatureId);
            if (entry.DatacubeId != 0u)
            {
                Datacube datacube = activator.DatacubeManager.GetDatacube((ushort)entry.DatacubeId, DatacubeType.Datacube);
                if (datacube == null)
                    activator.DatacubeManager.AddDatacube((ushort)entry.DatacubeId, progress);
                else
                {
                    datacube.Progress |= progress;
                    activator.DatacubeManager.SendDatacube(datacube);
                }
            }

            if (entry.DatacubeVolumeId != 0u)
            {
                Datacube datacube = activator.DatacubeManager.GetDatacube((ushort)entry.DatacubeVolumeId, DatacubeType.Journal);
                if (datacube == null)
                    activator.DatacubeManager.AddDatacubeVolume((ushort)entry.DatacubeVolumeId, progress);
                else
                {
                    datacube.Progress |= progress;
                    activator.DatacubeManager.SendDatacubeVolume(datacube);
                }
            }
        }
    }
}