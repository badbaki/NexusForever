﻿using System;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.WorldServer.Database;
using NexusForever.WorldServer.Database.Character.Model;
using NexusForever.WorldServer.Game.Entity;
using NexusForever.WorldServer.Game.Spell.Static;
using ItemEntity = NexusForever.WorldServer.Game.Entity.Item;

namespace NexusForever.WorldServer.Game.Spell
{
    public class UnlockedSpell : ISaveCharacter
    {
        public ulong Owner { get; }
        public SpellBaseInfo Info { get; }

        public byte Tier
        {
            get => tier;
            set
            {
                tier = value;
                saveMask |= UnlockedSpellSaveMask.Tier;
            }
        }

        private byte tier;

        public ItemEntity Item { get; }

        private UnlockedSpellSaveMask saveMask;

        /// <summary>
        /// Create a new <see cref="UnlockedSpell"/> from an existing database model.
        /// </summary>
        public UnlockedSpell(SpellBaseInfo info, CharacterSpell model, ItemEntity item)
        {
            Owner = model.Id;
            Info  = info;
            tier  = model.Tier;
            Item  = item;
        }

        /// <summary>
        /// Create a new <see cref="UnlockedSpell"/> from a <see cref="SpellBaseInfo"/>.
        /// </summary>
        public UnlockedSpell(Player player, SpellBaseInfo info, byte tier, ItemEntity item)
        {
            Owner = player.CharacterId;
            Info  = info ?? throw new ArgumentNullException();
            Tier  = tier;
            Item  = item;

            saveMask = UnlockedSpellSaveMask.Create;
        }

        public void Save(CharacterContext context)
        {
            if (saveMask == UnlockedSpellSaveMask.None)
                return;

            if ((saveMask & UnlockedSpellSaveMask.Create) != 0)
            {
                var model = new CharacterSpell
                {
                    Id           = Owner,
                    Spell4BaseId = Info.Entry.Id,
                    Tier         = tier
                };

                context.Add(model);
            }
            else
            {
                var model = new CharacterSpell
                {
                    Id           = Owner,
                    Spell4BaseId = Info.Entry.Id,
                };

                EntityEntry<CharacterSpell> entity = context.Attach(model);
                if ((saveMask & UnlockedSpellSaveMask.Tier) != 0)
                {
                    model.Tier = tier;
                    entity.Property(p => p.Tier).IsModified = true;
                }
            }

            saveMask = UnlockedSpellSaveMask.None;
        }

        /// <summary>
        /// Used for when the client does not have continuous casting enabled
        /// </summary>
        public void Cast(Player player)
        {
            if (player.HasSpell(Info.GetSpellInfo(Tier).Entry.Id, out Spell spell))
            {
                if ((spell.CastMethod == CastMethod.RapidTap || spell.CastMethod == CastMethod.ChargeRelease) && !spell.IsFinished)
                {
                    spell.Cast();
                    return;
                }
            }

            CastSpell(player);
        }

        /// <summary>
        /// Used for continuous casting when the client has it enabled, or spells with Cast Methods like ChargeRelease
        /// </summary>
        public void Cast(Player player, bool buttonPressed)
        {
            // TODO: Handle continuous casting of spell for Player if button remains depressed

            if (player.HasSpell(Info.GetSpellInfo(Tier).Entry.Id, out Spell spell))
            {
                if ((spell.CastMethod == CastMethod.RapidTap || spell.CastMethod == CastMethod.ChargeRelease) && !spell.IsFinished)
                {
                    spell.Cast();
                    return;
                }
            }

            // If the player depresses button after the spell had exceeded its threshold, don't try and recast the spell until button is pressed down again.
            if (!buttonPressed && (CastMethod)Info.Entry.CastMethod == CastMethod.ChargeRelease)
                return;

            CastSpell(player);
        }

        private void CastSpell(Player player)
        {
            player.CastSpell(new SpellParameters
            {
                SpellInfo = Info.GetSpellInfo(Tier),
                UserInitiatedSpellCast = true
            });
        }
    }
}
