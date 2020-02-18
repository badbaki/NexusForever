using NexusForever.Shared;
using NexusForever.Shared.GameTable;
using NexusForever.Shared.GameTable.Model;
using NexusForever.WorldServer.Game.CSI;
using NexusForever.WorldServer.Game.Entity.Network;
using NexusForever.WorldServer.Game.Entity.Network.Model;
using NexusForever.WorldServer.Game.Entity.Static;
using NexusForever.WorldServer.Game.Spell;
using NexusForever.WorldServer.Network.Message.Model;
using NLog;
using System;
using System.Numerics;
using EntityModel = NexusForever.WorldServer.Database.World.Model.Entity;

namespace NexusForever.WorldServer.Game.Entity
{
    [DatabaseEntity(EntityType.InstancePortal)]
    public class InstancePortal : UnitEntity
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        public uint RemainingTimeMs { get; private set; }
        public InstancePortalEntry InstancePortalEntry { get; private set; }
        public Creature2Entry CreatureEntry { get; private set; }

        public InstancePortal()
            : base(EntityType.InstancePortal)
        {
        }

        public override void Initialise(EntityModel model)
        {
            base.Initialise(model);

            CreatureEntry = GameTableManager.Instance.Creature2.GetEntry(CreatureId);
            if (CreatureEntry == null)
                throw new ArgumentNullException("creature2Entry");

            if (CreatureEntry.InstancePortalId == 0)
                throw new ArgumentOutOfRangeException("entry.InstancePortalId");

            InstancePortalEntry = GameTableManager.Instance.InstancePortal.GetEntry(CreatureEntry.InstancePortalId);
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new InstancePortalEntityModel
            {
                CreatureId = CreatureId,
                RemainingTimeMs = RemainingTimeMs
            };
        }

        public override void OnActivateCast(Player activator, uint interactionId)
        {


            // TODO: Handle casting activate spells at correct times. Additionally, ensure Prerequisites are met to cast.
            // Creature2Entry can contain up to 4 spells to activate and prerequisite spells to trigger.
            uint spell4Id = 116;
            if (CreatureEntry.Spell4IdActivate00 > 0)
                spell4Id = CreatureEntry.Spell4IdActivate00;

            SpellParameters parameters = new SpellParameters
            {
                PrimaryTargetId = Guid,
                ClientSideInteraction = new ClientSideInteraction(activator, this, interactionId),
                CastTimeOverride = CreatureEntry.ActivateSpellCastTime,
            };
            activator.CastSpell(spell4Id, parameters);
        }

        public override void OnActivateSuccess(Player activator)
        {
            if (InstancePortalEntry.Id == 181) // To SuperMall
            {
                WorldLocation2Entry entry = GameTableManager.Instance.WorldLocation2.GetEntry(47085);
                if (entry == null)
                    throw new ArgumentNullException("location2Entry");

                activator.CastSpell(83792, new SpellParameters
                {
                    UserInitiatedSpellCast = false,
                    CastTimeOverride = 0,
                    OnExecuteComplete = () =>
                    {
                        var rotation = new Quaternion(entry.Facing0, entry.Facing1, entry.Facing2, entry.Facing3);
                        activator.Rotation = rotation.ToEulerDegrees();
                        activator.TeleportTo((ushort)entry.WorldId, entry.Position0, entry.Position1, entry.Position2);
                    }
                });
            }

            if (InstancePortalEntry.Id == 182) // To Event Area
            {
                activator.CastSpell(83792, new SpellParameters
                {
                    UserInitiatedSpellCast = false,
                    CastTimeOverride = 0,
                    OnExecuteComplete = () =>
                    {
                        if (activator.Faction1 == Faction.Exile)
                            activator.TeleportTo(51, 3716.477f, -834.4801f, -1645.082f);

                        if (activator.Faction1 == Faction.Dominion)
                            activator.TeleportTo(22, -3493.419f, -882.8023f, -991.9232f);
                    }
                });
            }
        }
    }
}