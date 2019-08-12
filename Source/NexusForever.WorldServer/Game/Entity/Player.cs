using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Shared.Configuration;
using NexusForever.Shared.Database;
using NexusForever.Shared.Database.Auth;
using NexusForever.Shared.Database.Auth.Model;
using NexusForever.Shared.Game.Events;
using NexusForever.Shared.GameTable;
using NexusForever.Shared.GameTable.Model;
using NexusForever.Shared.GameTable.Static;
using NexusForever.WorldServer.Game.Spell;
using NexusForever.WorldServer.Game.Spell.Static;
using NexusForever.Shared.Network;
using NexusForever.WorldServer.Database;
using NexusForever.WorldServer.Database.Character;
using NexusForever.WorldServer.Database.Character.Model;
using NexusForever.WorldServer.Game.CharacterCache;
using NexusForever.WorldServer.Game.Contact;
using NexusForever.WorldServer.Game.Entity.Network;
using NexusForever.WorldServer.Game.Entity.Network.Model;
using NexusForever.WorldServer.Game.Entity.Static;
using NexusForever.WorldServer.Game.Guild;
using NexusForever.WorldServer.Game.Guild.Static;
using NexusForever.WorldServer.Game.Map;
using NexusForever.WorldServer.Game.Quest.Static;
using NexusForever.WorldServer.Game.Setting;
using NexusForever.WorldServer.Game.Setting.Static;
using NexusForever.WorldServer.Game.Social;
using NexusForever.WorldServer.Game.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Model;
using NexusForever.WorldServer.Network.Message.Model.Shared;

namespace NexusForever.WorldServer.Game.Entity
{
    public class Player : UnitEntity, ISaveAuth, ISaveCharacter, ICharacter
    {
        // TODO: move this to the config file
        private const double SaveDuration = 60d;

        public ulong CharacterId { get; }
        public string Name { get; }
        public Sex Sex { get; private set; }
        public Race Race { get; private set; }
        public Class Class { get; }
        public Faction Faction { get; }
        public List<float> Bones { get; } = new List<float>();

        public uint TotalXp
        {
            get => totalXp;
            set
            {
                totalXp = value;
                saveMask |= PlayerSaveMask.Xp;
            }
        }

        private uint totalXp;
        public uint XpToNextLevel { get; private set; }

        public Path Path
        {
            get => path;
            set
            {
                path = value;
                saveMask |= PlayerSaveMask.Path;
            }
        }
        private Path path;

        public sbyte CostumeIndex
        {
            get => costumeIndex;
            set
            {
                costumeIndex = value;
                saveMask |= PlayerSaveMask.Costume;
            }
        }
        private sbyte costumeIndex;

        public InputSets InputKeySet
        {
            get => inputKeySet;
            set
            {
                inputKeySet = value;
                saveMask |= PlayerSaveMask.InputKeySet;
            }
        }
        private InputSets inputKeySet;

        public byte InnateIndex
        {
            get => innateIndex;
            set
            {
                innateIndex = value;
                saveMask |= PlayerSaveMask.Innate;
            }
        }
        private byte innateIndex;

        public DateTime CreateTime { get; }
        public double TimePlayedTotal { get; private set; }
        public double TimePlayedLevel { get; private set; }
        public double TimePlayedSession { get; private set; }

        /// <summary>
        /// Guid of the <see cref="WorldEntity"/> that currently being controlled by the <see cref="Player"/>.
        /// </summary>
        public uint ControlGuid { get; private set; }

        /// <summary>
        /// Guid of the <see cref="Vehicle"/> the <see cref="Player"/> is a passenger on.
        /// </summary>
        public uint VehicleGuid
        {
            get => MovementManager.GetPlatform() ?? 0u;
            set => MovementManager.SetPlatform(value);
        }

        /// <summary>
        /// Guid of the <see cref="VanityPet"/> currently summoned by the <see cref="Player"/>.
        /// </summary>
        public uint PetGuid { get; set; }

        public List<ulong> IgnoreList { get; set; }
        public bool IsIgnoring(ulong value) => IgnoreList.Contains(value);

        public WorldSession Session { get; }
        public bool IsLoading { get; private set; } = true;

        public Inventory Inventory { get; }
        public CurrencyManager CurrencyManager { get; }
        public PathManager PathManager { get; }
        public TitleManager TitleManager { get; }
        public SpellManager SpellManager { get; }
        public CostumeManager CostumeManager { get; }
        public PetCustomisationManager PetCustomisationManager { get; }
        public KeybindingManager KeybindingManager { get; }
        public DatacubeManager DatacubeManager { get; }
        public MailManager MailManager { get; }
        public ZoneMapManager ZoneMapManager { get; }
        public QuestManager QuestManager { get; }

        public VendorInfo SelectedVendorInfo { get; set; } // TODO unset this when too far away from vendor

        private UpdateTimer saveTimer = new UpdateTimer(SaveDuration);
        private PlayerSaveMask saveMask;

        private LogoutManager logoutManager;
        private PendingTeleport pendingTeleport;

        private bool loggedIn = false;

        public ulong GuildId = 0;
        public List<ulong> GuildMemberships = new List<ulong>();
        public GuildInvite PendingGuildInvite;
        public ulong GuildAffiliation
        {
            get => guildAffiliation;
            set
            {
                if (guildAffiliation != value)
                {
                    guildAffiliation = value;
                    saveMask |= PlayerSaveMask.Affiliation;
                }
            }
        }
        private ulong guildAffiliation;

        public GuildHolomark GuildHolomarkMask
        {
            get => guildHolomarkMask;
            set
            {
                if (guildHolomarkMask != value)
                {
                    guildHolomarkMask = value;
                    saveMask |= PlayerSaveMask.Holomark;
                }
            }
        }
        private GuildHolomark guildHolomarkMask;

        public float GetOnlineStatus() => 0f;

        /// <summary>
        /// Character Customisation models. Stored for modification purposes.
        /// </summary>
        private Dictionary</*label*/uint, Customisation> characterCustomisations = new Dictionary<uint, Customisation>();
        private HashSet<Customisation> deletedCharacterCustomisations = new HashSet<Customisation>();
        private Dictionary<ItemSlot, Appearance> characterAppearances = new Dictionary<ItemSlot, Appearance>();
        private HashSet<Appearance> deletedCharacterAppearances = new HashSet<Appearance>();
        private List<Bone> characterBones = new List<Bone>();
        private HashSet<Bone> deletedCharacterBones = new HashSet<Bone>();

        public Player(WorldSession session, Character model)
            : base(EntityType.Player)
        {
            ActivationRange = BaseMap.DefaultVisionRange;

            Session         = session;

            CharacterId     = model.Id;
            Name            = model.Name;
            Sex             = (Sex)model.Sex;
            Race            = (Race)model.Race;
            Class           = (Class)model.Class;
            Path            = (Path)model.ActivePath;
            CostumeIndex    = model.ActiveCostumeIndex;
            InputKeySet     = (InputSets)model.InputKeySet;
            Faction         = (Faction)model.FactionId;
            Faction1        = (Faction)model.FactionId;
            Faction2        = (Faction)model.FactionId;
            guildAffiliation = model.GuildAffiliation;
            guildHolomarkMask = (GuildHolomark)model.GuildHolomarkMask;
            TotalXp         = model.TotalXp;
            XpToNextLevel   = GameTableManager.XpPerLevel.Entries.FirstOrDefault(c => c.Id == Level + 1).MinXpForLevel;
            innateIndex     = model.InnateIndex;

            CreateTime      = model.CreateTime;
            TimePlayedTotal = model.TimePlayedTotal;
            TimePlayedLevel = model.TimePlayedLevel;

            // managers
            CostumeManager          = new CostumeManager(this, session.Account, model);
            Inventory               = new Inventory(this, model);
            CurrencyManager         = new CurrencyManager(this, model);
            PathManager             = new PathManager(this, model);
            TitleManager            = new TitleManager(this, model);
            SpellManager            = new SpellManager(this, model);
            PetCustomisationManager = new PetCustomisationManager(this, model);
            KeybindingManager       = new KeybindingManager(this, session.Account, model);
            DatacubeManager         = new DatacubeManager(this, model);
            MailManager             = new MailManager(this, model);
            ZoneMapManager          = new ZoneMapManager(this, model);
            QuestManager            = new QuestManager(this, model);
            IgnoreList              = ContactManager.GetIgnoreList(model);

            // temp
            Properties.Add(Property.BaseHealth, new PropertyValue(Property.BaseHealth, 200f, 800f));
            Properties.Add(Property.ShieldCapacityMax, new PropertyValue(Property.ShieldCapacityMax, 0f, 450f));
            Properties.Add(Property.MoveSpeedMultiplier, new PropertyValue(Property.MoveSpeedMultiplier, 1.5f, 1.5f));
            Properties.Add(Property.JumpHeight, new PropertyValue(Property.JumpHeight, 5f, 5f));
            Properties.Add(Property.GravityMultiplier, new PropertyValue(Property.GravityMultiplier, 1f, 1f));
            Properties.Add(Property.MountSpeedMultiplier, new PropertyValue(Property.MountSpeedMultiplier, 3f, 3f));
            // sprint
            Properties.Add(Property.ResourceMax0, new PropertyValue(Property.ResourceMax0, 500f, 500f));
            // dash
            Properties.Add(Property.ResourceMax7, new PropertyValue(Property.ResourceMax7, 200f, 200f));

            Costume costume = null;
            if (CostumeIndex >= 0)
                costume = CostumeManager.GetCostume((byte)CostumeIndex);

            SetAppearance(Inventory.GetItemVisuals(costume));
            SetAppearance(model.CharacterAppearance
                .Select(a => new ItemVisual
                {
                    Slot      = (ItemSlot)a.Slot,
                    DisplayId = a.DisplayId
                }));

            // Store Character Customisation models in memory so if changes occur, they can be removed.
            foreach (CharacterAppearance characterAppearance in model.CharacterAppearance)
                characterAppearances.Add((ItemSlot)characterAppearance.Slot, new Appearance(characterAppearance));

            foreach (CharacterCustomisation characterCustomisation in model.CharacterCustomisation)
                characterCustomisations.Add(characterCustomisation.Label, new Customisation(characterCustomisation));

            foreach (CharacterBone bone in model.CharacterBone.OrderBy(bone => bone.BoneIndex))
            {
                Bones.Add(bone.Bone);
                characterBones.Add(new Bone(bone));
            }

            foreach (CharacterStats statModel in model.CharacterStats)
                stats.Add((Stat)statModel.Stat, new StatValue(statModel));

            SetStat(Stat.Sheathed, 1u);

            // temp
            SetStat(Stat.Dash, 200F);
            // sprint
            SetStat(Stat.Resource0, 500f);
            SetStat(Stat.Shield, 450u);

            CharacterManager.RegisterPlayer(this);
            GlobalGuildManager.OnPlayerLogin(Session, this);
        }

        public override void Update(double lastTick)
        {
            if (logoutManager != null)
            {
                // don't process world updates while logout is finalising
                if (logoutManager.ReadyToLogout)
                    return;

                logoutManager.Update(lastTick);
            }

            base.Update(lastTick);
            TitleManager.Update(lastTick);
            SpellManager.Update(lastTick);
            CostumeManager.Update(lastTick);
            QuestManager.Update(lastTick);

            saveTimer.Update(lastTick);
            if (saveTimer.HasElapsed)
            {
                double timeSinceLastSave = GetTimeSinceLastSave();
                TimePlayedSession += timeSinceLastSave;
                TimePlayedLevel += timeSinceLastSave;
                TimePlayedTotal += timeSinceLastSave;

                Save();
            }
        }

        /// <summary>
        /// Save <see cref="Account"/> and <see cref="Character"/> to the database.
        /// </summary>
        public void Save(Action callback = null)
        {
            Session.EnqueueEvent(new TaskEvent(AuthDatabase.Save(Save),
            () =>
            {
                Session.EnqueueEvent(new TaskEvent(CharacterDatabase.Save(Save),
                () =>
                {
                    callback?.Invoke();
                    Session.CanProcessPackets = true;
                    saveTimer.Resume();
                }));
            }));

            saveTimer.Reset(false);

            // prevent packets from being processed until asynchronous player save task is complete
            Session.CanProcessPackets = false;
        }

        protected override IEntityModel BuildEntityModel()
        {
            PlayerEntityModel playerEntityModel = new PlayerEntityModel
            {
                Id       = CharacterId,
                RealmId  = WorldServer.RealmId,
                Name     = Name,
                Race     = Race,
                Class    = Class,
                Sex      = Sex,
                Bones    = Bones,
                Title    = TitleManager.ActiveTitleId,
                GuildIds = GuildMemberships,
                PvPFlag  = PvPFlag.Disabled
            };

            if (GuildAffiliation > 0)
            {
                GuildBase guild = GlobalGuildManager.GetGuild(GuildAffiliation);
                if (guild.GetMember(CharacterId) != null)
                {
                    playerEntityModel.GuildName = guild.Name;
                    playerEntityModel.GuildType = guild.Type;
                }
                else
                    GuildAffiliation = 0;
            }

            return playerEntityModel;
        }

        public override void OnAddToMap(BaseMap map, uint guid, Vector3 vector)
        {
            IsLoading = true;

            Session.EnqueueMessageEncrypted(new ServerChangeWorld
            {
                WorldId  = (ushort)map.Entry.Id,
                Position = new Position(vector)
            });

            base.OnAddToMap(map, guid, vector);
            map.OnAddToMap(this);

            SendPacketsAfterAddToMap();

            Session.EnqueueMessageEncrypted(new ServerPlayerEnteredWorld());

            IsLoading = false;

            if (!loggedIn)
                OnLogin();
        }

        public override void OnRelocate(Vector3 vector)
        {
            base.OnRelocate(vector);
            saveMask |= PlayerSaveMask.Location;

            // TODO: remove this once pathfinding is implemented
            if (PetGuid > 0)
                Map.EnqueueRelocate(GetVisible<VanityPet>(PetGuid), vector);

            ZoneMapManager.OnRelocate(vector);
        }

        protected override void OnZoneUpdate()
        {
            if (Zone != null)
            {
                TextTable tt = GameTableManager.GetTextTable(Language.English);

                /* Shhh quiet zone spam
                Session.EnqueueMessageEncrypted(new ServerChat
                {
                    Guid    = Session.Player.Guid,
                    Channel = ChatChannel.System,
                    Text    = $"New Zone: ({Zone.Id}){tt.GetEntry(Zone.LocalizedTextIdName)}"
                });
                */

                uint tutorialId = AssetManager.GetTutorialIdForZone(Zone.Id);
                if (tutorialId > 0)
                {
                    Session.EnqueueMessageEncrypted(new ServerTutorial
                    {
                        TutorialId = tutorialId
                    });
                }

                QuestManager.ObjectiveUpdate(QuestObjectiveType.EnterZone, Zone.Id, 1);
            }

            ZoneMapManager.OnZoneUpdate();
        }

        private void SendPacketsAfterAddToMap()
        {
            GlobalGuildManager.SendInitialPackets(Session);
            SendInGameTime();
            PathManager.SendInitialPackets();

            BuybackManager.SendBuybackItems(this);

            ContactManager.OnLogin(Session);

            Session.EnqueueMessageEncrypted(new ServerHousingNeighbors());
            Session.EnqueueMessageEncrypted(new Server00F1());
            SetControl(this);

            // TODO: Move to Unlocks/Rewards Handler. A lot of these are tied to Entitlements which display in the character sheet, but don't actually unlock anything without this packet.
            Session.EnqueueMessageEncrypted(new ServerRewardPropertySet
            {
                Variables = new List<ServerRewardPropertySet.RewardProperty>
                {
                    new ServerRewardPropertySet.RewardProperty
                    {
                        Id    = RewardProperty.CostumeSlots,
                        Type  = 1,
                        Value = CostumeManager.CostumeCap
                    },
                    new ServerRewardPropertySet.RewardProperty
                    {
                        Id    = RewardProperty.ExtraDecorSlots,
                        Type  = 1,
                        Value = 3000
                    },
                    new ServerRewardPropertySet.RewardProperty
                    {
                        Id    = RewardProperty.GuildCreateOrInviteAccess,
                        Type  = 1,
                        Value = 1
                    },
                    new ServerRewardPropertySet.RewardProperty
                    {
                        Id    = RewardProperty.GuildHolomarkUnlimited,
                        Type  = 1,
                        Value = 1
                    },
                    new ServerRewardPropertySet.RewardProperty
                    {
                        Id    = RewardProperty.BagSlots,
                        Type  = 1,
                        Value = 4
                    },
                    new ServerRewardPropertySet.RewardProperty
                    {
                        Id    = RewardProperty.Trading,
                        Type  = 1,
                        Value = 1
                    }
                }
            });

            CostumeManager.SendInitialPackets();

            var playerCreate = new ServerPlayerCreate
            {
                ItemProficiencies = GetItemProficiences(),
                FactionData       = new ServerPlayerCreate.Faction
                {
                    FactionId = Faction1, // This does not do anything for the player's "main" faction. Exiles/Dominion
                },
                ActiveCostumeIndex = CostumeIndex,
                InputKeySet = (uint)InputKeySet,
                Xp = TotalXp
            };

            foreach (Currency currency in CurrencyManager)
                playerCreate.Money[(byte)currency.Id - 1] = currency.Amount;

            foreach (Item item in Inventory
                .Where(b => b.Location != InventoryLocation.Ability)
                .SelectMany(i => i))
            {
                playerCreate.Inventory.Add(new InventoryItem
                {
                    Item   = item.BuildNetworkItem(),
                    Reason = ItemUpdateReason.NoReason
                });
            }

            playerCreate.SpecIndex = SpellManager.ActiveActionSet;

            Session.EnqueueMessageEncrypted(playerCreate);

            TitleManager.SendTitles();
            SpellManager.SendInitialPackets();
            PetCustomisationManager.SendInitialPackets();
            KeybindingManager.SendInitialPackets();
            DatacubeManager.SendInitialPackets();
            MailManager.SendInitialPackets();
            ZoneMapManager.SendInitialPackets();
            Session.AccountCurrencyManager.SendInitialPackets();
            QuestManager.SendInitialPackets();
            SocialManager.JoinChatChannels(Session);
            Session.EnqueueMessageEncrypted(new ServerPlayerInnate
            {
                InnateIndex = InnateIndex
            });
        }

        public ItemProficiency GetItemProficiences()
        {
            ClassEntry classEntry = GameTableManager.Class.GetEntry((ulong)Class);
            return (ItemProficiency)classEntry.StartingItemProficiencies;

            //TODO: Store proficiences in DB table and load from there. Do they change ever after creation? Perhaps something for use on custom servers?
        }

        public override void OnRemoveFromMap()
        {
            DestroyDependents();

            base.OnRemoveFromMap();

            if (pendingTeleport != null)
            {
                MapManager.AddToMap(this, pendingTeleport.Info, pendingTeleport.Vector);
                pendingTeleport = null;
            }
        }

        public override void AddVisible(GridEntity entity)
        {
            base.AddVisible(entity);
            Session.EnqueueMessageEncrypted(((WorldEntity)entity).BuildCreatePacket());

            if (entity is Player player)
                player.PathManager.SendSetUnitPathTypePacket();

            if (entity == this)
            {
                Session.EnqueueMessageEncrypted(new ServerPlayerChanged
                {
                    Guid     = entity.Guid,
                    Unknown1 = 1
                });
            }
        }

        public override void RemoveVisible(GridEntity entity)
        {
            base.RemoveVisible(entity);

            if (entity != this)
            {
                Session.EnqueueMessageEncrypted(new ServerEntityDestroy
                {
                    Guid     = entity.Guid,
                    Unknown0 = true
                });
            }
        }

        /// <summary>
        /// Set the <see cref="WorldEntity"/> that currently being controlled by the <see cref="Player"/>.
        /// </summary>
        public void SetControl(WorldEntity entity)
        {
            ControlGuid = entity.Guid;
            entity.ControllerGuid = Guid;

            Session.EnqueueMessageEncrypted(new ServerMovementControl
            {
                Ticket    = 1,
                Immediate = true,
                UnitId    = entity.Guid
            });
        }

        /// <summary>
        /// Start delayed logout with optional supplied time and <see cref="LogoutReason"/>.
        /// </summary>
        public void LogoutStart(double timeToLogout = 3d, LogoutReason reason = LogoutReason.None, bool requested = true)
        {
            if (logoutManager != null)
                return;

            logoutManager = new LogoutManager(timeToLogout, reason, requested);

            Session.EnqueueMessageEncrypted(new ServerLogoutUpdate
            {
                TimeTillLogout     = (uint)timeToLogout * 1000,
                Unknown0           = false,
                SignatureBonusData = new ServerLogoutUpdate.SignatureBonuses
                {
                    // see FillSignatureBonuses in ExitWindow.lua for more information
                    Xp                = 0,
                    ElderPoints       = 0,
                    Currencies        = new ulong[15],
                    AccountCurrencies = new ulong[19]
                }
            });
        }

        /// <summary>
        /// Cancel the current logout, this will fail if the timer has already elapsed.
        /// </summary>
        public void LogoutCancel()
        {
            // can't cancel logout if timer has already elapsed
            if (logoutManager?.ReadyToLogout ?? false)
                return;

            logoutManager = null;
        }

        /// <summary>
        /// Finishes the current logout, saving and cleaning up the <see cref="Player"/> before redirect to the character screen.
        /// </summary>
        public void LogoutFinish()
        {
            if (logoutManager == null)
                throw new InvalidPacketValueException();

            Session.EnqueueMessageEncrypted(new ServerLogout
            {
                Requested = logoutManager.Requested,
                Reason    = logoutManager.Reason
            });

            CleanUp();
        }

        /// <summary>
        /// Save to the database, remove from the world and release from parent <see cref="WorldSession"/>.
        /// </summary>
        public void CleanUp()
        {
            // CharacterManager must deregister player first so other events see the user as offline and with relevant data being final
            CharacterManager.DeregisterPlayer(this);

            GlobalGuildManager.OnPlayerLogout(Session, this);
            CleanupManager.Track(Session.Account);

            try
            {
                Save(() =>
                {
                    RemoveFromMap();
                    SocialManager.LeaveChatChannels(Session);
                    ContactManager.OnLogout(Session);
                    Session.Player = null;
                });
            }
            finally
            {
                CleanupManager.Untrack(Session.Account);
            }
        }

        private void OnLogin()
        {
            loggedIn = true;

            var motd = ConfigurationManager<WorldServerConfiguration>.Config.MessageOfTheDay;
            if (motd.Length > 0)
                SocialManager.SendMessage(Session, "MOTD: " + motd, channel: ChatChannel.Realm);
        }

        /// <summary>
        /// Teleport <see cref="Player"/> to supplied location.
        /// </summary>
        public void TeleportTo(ushort worldId, float x, float y, float z, uint instanceId = 0u, ulong residenceId = 0ul)
        {
            WorldEntry entry = GameTableManager.World.GetEntry(worldId);
            if (entry == null)
                throw new ArgumentException();

            TeleportTo(entry, new Vector3(x, y, z), instanceId, residenceId);
        }

        /// <summary>
        /// Teleport <see cref="Player"/> to supplied location.
        /// </summary>
        public void TeleportTo(WorldEntry entry, Vector3 vector, uint instanceId = 0u, ulong residenceId = 0ul)
        {
            if (DisableManager.Instance.IsDisabled(DisableType.World, entry.Id))
            {
                SendSystemMessage($"Unable to teleport to world {entry.Id} because it is disabled.");
                return;
            }

            if (Map?.Entry.Id == entry.Id)
            {
                // TODO: don't remove player from map if it's the same as destination
            }

            var info = new MapInfo(entry, instanceId, residenceId);
            pendingTeleport = new PendingTeleport(info, vector);
            RemoveFromMap();
        }

        /// <summary>
        /// Returns whether this <see cref="Player"/> is allowed to summon or be added to a mount
        /// </summary>
        public bool CanMount()
        {
            return VehicleGuid == 0u && pendingTeleport == null && logoutManager == null;
        }

        /// <summary>
        /// Dismounts this <see cref="Player"/> from a vehicle that it's attached to
        /// </summary>
        public void Dismount()
        {
            if (VehicleGuid != 0u)
            {
                Vehicle vehicle = GetVisible<Vehicle>(VehicleGuid);
                vehicle.PassengerRemove(this);
            }
        }

        /// <summary>
        /// Remove all entities associated with the <see cref="Player"/>
        /// </summary>
        private void DestroyDependents()
        {
            // TODO: Enqueue re-creation of necessary entities
            if (VehicleGuid != 0u)
            {
                Vehicle vehicle = GetVisible<Vehicle>(VehicleGuid);
                if (vehicle != null)
                    vehicle.Destroy();
                VehicleGuid = 0u;
            }

            // TODO: Remove pets, scanbots, vanity pets
        }

        /// <summary>
        /// Used to send the current in game time to this player
        /// </summary>
        private void SendInGameTime()
        {
            uint lengthOfInGameDayInSeconds = ConfigurationManager<WorldServerConfiguration>.Config.LengthOfInGameDay;
            if (lengthOfInGameDayInSeconds == 0u)
                lengthOfInGameDayInSeconds = (uint)TimeSpan.FromHours(3.5d).TotalSeconds; // Live servers were 3.5h per in game day

            double timeOfDay = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds / lengthOfInGameDayInSeconds % 1;

            Session.EnqueueMessageEncrypted(new ServerTimeOfDay
            {
                TimeOfDay = (uint)(timeOfDay * TimeSpan.FromDays(1).TotalSeconds),
                LengthOfDay = lengthOfInGameDayInSeconds
            });
        }

        /// <summary>
        /// Reset and restore default appearance for <see cref="Player"/>.
        /// </summary>
        public void ResetAppearance()
        {
            DisplayInfo = 0;

            EnqueueToVisible(new ServerEntityVisualUpdate
            {
                UnitId      = Guid,
                Race        = (byte)Race,
                Sex         = (byte)Sex,
                ItemVisuals = GetAppearance().ToList()
            }, true);
        }

        /// <summary>
        /// Send <see cref="GenericError"/> to <see cref="Player"/>.
        /// </summary>
        public void SendGenericError(GenericError error)
        {
            Session.EnqueueMessageEncrypted(new ServerGenericError
            {
                Error = error
            });
        }

        /// <summary>
        /// Send message to <see cref="Player"/> using the <see cref="ChatChannel.System"/> channel.
        /// </summary>
        /// <param name="text"></param>
        public void SendSystemMessage(string text)
        {
            Session.EnqueueMessageEncrypted(new ServerChat
            {
                Channel = ChatChannel.System,
                Text    = text
            });
        }

        /// Grants <see cref="Player"/> the supplied experience, handling level up if necessary.
        /// </summary>
        /// <param name="xp">Experience to grant</param>
        /// <param name="reason"><see cref="ExpReason"/> for the experience grant</param>
        public void GrantXp(uint xp, ExpReason reason = ExpReason.KillCreature)
        {
            const uint maxLevel = 50;

            if (xp < 1)
                return;

            //if (!IsAlive)
            //    return;

            if (Level >= maxLevel)
                return;

            // TODO: Signature Bonus XP Calculation
            uint signatureXp = 0;

            // TODO: Rest XP Calculation
            uint restXp = 0;

            uint currentLevel = Level;
            uint currentXp = TotalXp;
            uint xpToNextLevel = XpToNextLevel;
            uint totalXp = xp + currentXp + signatureXp + restXp;

            Session.EnqueueMessageEncrypted(new ServerExperienceGained
            {
                TotalXpGained = xp,
                RestXpAmount = restXp,
                SignatureXpAmount = signatureXp,
                Reason = reason
            });

            while (totalXp >= xpToNextLevel && currentLevel < maxLevel)// WorldServer.Rules.MaxLevel)
            {
                totalXp -= xpToNextLevel;

                if (currentLevel < maxLevel)
                    GrantLevel((byte)(Level + 1));

                currentLevel = Level;
                xpToNextLevel = XpToNextLevel;
            }

            SetXp(xp + currentXp + signatureXp + restXp);
        }

        /// <summary>
        /// Sets <see cref="Player"/> <see cref="TotalXp"/> to supplied value
        /// </summary>
        /// <param name="xp"></param>
        private void SetXp(uint xp)
        {
            TotalXp = xp;
        }

        /// <summary>
        /// Grants <see cref="Player"/> the supplied level and adjusts XP accordingly
        /// </summary>
        /// <param name="newLevel">New level to be set</param>
        public void GrantLevel(byte newLevel)
        {
            uint oldLevel = Level;

            if (newLevel == oldLevel)
                return;

            Level = newLevel;
            XpToNextLevel = GameTableManager.XpPerLevel.GetEntry((ulong)newLevel + 1).MinXpForLevel;

            // Grant Rewards for level up
            SpellManager.GrantSpells();
            // Unlock LAS slots
            // Unlock AMPs
            // Add feature access

            // Level up effect is triggered by a Client request
        }

        /// <summary>
        /// Sets <see cref="Player"/> to the supplied level and adjusts XP accordingly. Mainly for use with GM commands.
        /// </summary>
        /// <param name="newLevel">New level to be set</param>
        /// <param name="reason"><see cref="ExpReason"/> for the level grant</param>
        public void SetLevel(byte newLevel, ExpReason reason = ExpReason.Cheat)
        {
            uint oldLevel = Level;

            if (newLevel == oldLevel)
                return;

            uint newXp = GameTableManager.XpPerLevel.GetEntry(newLevel).MinXpForLevel;
            Session.EnqueueMessageEncrypted(new ServerExperienceGained
            {
                TotalXpGained = newXp - TotalXp,
                RestXpAmount = 0,
                SignatureXpAmount = 0,
                Reason = reason
            });
            SetXp(newXp);

            GrantLevel(newLevel);
        }

        /// <summary>
        /// Modifies the appearance customisation of this <see cref="Player"/>. Called directly by a packet handler.
        /// </summary>
        public void SetCharacterCustomisation(Dictionary<uint, uint> customisations, List<float> bones, Race newRace, Sex newSex, bool usingServiceTokens)
        {
            // Set Sex and Race
            Sex = newSex;
            Race = newRace; // TODO: Ensure new Race is on the same faction

            List<ItemSlot> itemSlotsModified = new List<ItemSlot>();
            // Build models for all new customisations and store in customisations caches. The client sends through everything needed on every change.
            foreach ((uint label, uint value) in customisations)
            {
                if (characterCustomisations.TryGetValue(label, out Customisation customisation))
                    customisation.Value = value;
                else
                    characterCustomisations.TryAdd(label, new Customisation(CharacterId, label, value));

                foreach (CharacterCustomizationEntry entry in AssetManager.GetCharacterCustomisation(customisations, (uint)newRace, (uint)newSex, label, value))
                {
                    if (characterAppearances.TryGetValue((ItemSlot)entry.ItemSlotId, out Appearance appearance))
                        appearance.DisplayId = (ushort)entry.ItemDisplayId;
                    else
                        characterAppearances.TryAdd((ItemSlot)entry.ItemSlotId, new Appearance(CharacterId, (ItemSlot)entry.ItemSlotId, (ushort)entry.ItemDisplayId));

                    // This is to track slots which are modified
                    itemSlotsModified.Add((ItemSlot)entry.ItemSlotId);
                }
            }

            for (int i = 0; i < bones.Count; i++)
            {
                if (i > characterBones.Count - 1)
                    characterBones.Add(new Bone(CharacterId, (byte)i, bones[i]));
                else
                {
                    var bone = characterBones.FirstOrDefault(x => x.BoneIndex == i);
                    if (bone != null)
                        bone.BoneValue = bones[i];
                }
            }

            // Cleanup the unused customisations
            foreach (ItemSlot slot in characterAppearances.Keys.Except(itemSlotsModified).ToList())
            {
                if (characterAppearances.TryGetValue(slot, out Appearance appearance))
                {
                    characterAppearances.Remove(slot);
                    appearance.Delete();
                    deletedCharacterAppearances.Add(appearance);
                }
            }
            foreach (uint key in characterCustomisations.Keys.Except(customisations.Keys).ToList())
            {
                if (characterCustomisations.TryGetValue(key, out Customisation customisation))
                {
                    characterCustomisations.Remove(key);
                    customisation.Delete();
                    deletedCharacterCustomisations.Add(customisation);
                }
            }
            if (Bones.Count > bones.Count)
            {
                for (int i = Bones.Count; i >= bones.Count; i--)
                {
                    Bone bone = characterBones[i];

                    if (bone != null)
                    {
                        characterBones.RemoveAt(i);
                        bone.Delete();
                        deletedCharacterBones.Add(bone);
                    }
                }
            }

            // Update Player appearance values
            SetAppearance(characterAppearances.Values
                .Select(a => new ItemVisual
                {
                    Slot = a.ItemSlot,
                    DisplayId = a.DisplayId
                }));

            Bones.Clear();
            foreach (Bone bone in characterBones.OrderBy(bone => bone.BoneIndex))
                Bones.Add(bone.BoneValue);

            // Update surrounding entities, including the player, with new appearance
            EmitVisualUpdate();

            // TODO: Charge the player for service

            // Enqueue the appearance changes to be saved to the DB.
            saveMask |= PlayerSaveMask.Appearance;
        }

        /// <summary>
        /// Update surrounding <see cref="WorldEntity"/>, including the <see cref="Player"/>, with a fresh appearance dataset.
        /// </summary>
        public void EmitVisualUpdate()
        {
            Costume costume = null;
            if (CostumeIndex >= 0)
                costume = CostumeManager.GetCostume((byte)CostumeIndex);

            var entityVisualUpdate = new ServerEntityVisualUpdate
            {
                UnitId = Guid,
                Sex = (byte)Sex,
                Race = (byte)Race
            };

            foreach (Appearance characterAppearance in characterAppearances.Values)
                entityVisualUpdate.ItemVisuals.Add(new ItemVisual
                {
                    Slot = characterAppearance.ItemSlot,
                    DisplayId = characterAppearance.DisplayId
                });

            foreach (var itemVisual in Inventory.GetItemVisuals(costume))
                entityVisualUpdate.ItemVisuals.Add(itemVisual);

            EnqueueToVisible(entityVisualUpdate, true);

            EnqueueToVisible(new ServerEntityBoneUpdate
            {
                UnitId = Guid,
                Bones = Bones.ToList()
            }, true);
        }

        public void Save(AuthContext context)
        {
            Session.GenericUnlockManager.Save(context);
            Session.AccountCurrencyManager.Save(context);
            CostumeManager.Save(context);
            KeybindingManager.Save(context);
        }

        public void Save(CharacterContext context)
        {
            var model = new Character
            {
                Id = CharacterId
            };

            EntityEntry<Character> entity = context.Attach(model);

            if (saveMask != PlayerSaveMask.None)
            {
                if ((saveMask & PlayerSaveMask.Location) != 0)
                {
                    model.LocationX = Position.X;
                    entity.Property(p => p.LocationX).IsModified = true;

                    model.LocationY = Position.Y;
                    entity.Property(p => p.LocationY).IsModified = true;

                    model.LocationZ = Position.Z;
                    entity.Property(p => p.LocationZ).IsModified = true;

                    model.WorldId = (ushort)Map.Entry.Id;
                    entity.Property(p => p.WorldId).IsModified = true;

                    model.WorldZoneId = (ushort)Zone.Id;
                    entity.Property(p => p.WorldZoneId).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Path) != 0)
                {
                    model.ActivePath = (uint)Path;
                    entity.Property(p => p.ActivePath).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Costume) != 0)
                {
                    model.ActiveCostumeIndex = CostumeIndex;
                    entity.Property(p => p.ActiveCostumeIndex).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.InputKeySet) != 0)
                {
                    model.InputKeySet = (sbyte)InputKeySet;
                    entity.Property(p => p.InputKeySet).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Affiliation) != 0)
                {
                    model.GuildAffiliation = GuildAffiliation;
                    entity.Property(p => p.GuildAffiliation).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Holomark) != 0)
                {
                    model.GuildHolomarkMask = Convert.ToByte(GuildHolomarkMask);
                    entity.Property(p => p.GuildHolomarkMask).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Xp) != 0)
                {
                    model.TotalXp = TotalXp;
                    entity.Property(p => p.TotalXp).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Innate) != 0)
                {
                    model.InnateIndex = InnateIndex;
                    entity.Property(p => p.InnateIndex).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Appearance) != 0)
                {
                    model.Race = (byte)Race;
                    entity.Property(p => p.Race).IsModified = true;

                    model.Sex = (byte)Sex;
                    entity.Property(p => p.Sex).IsModified = true;

                    foreach (Appearance characterAppearance in deletedCharacterAppearances)
                        characterAppearance.Save(context);
                    foreach (Bone characterBone in deletedCharacterBones)
                        characterBone.Save(context);
                    foreach (Customisation characterCustomisation in deletedCharacterCustomisations)
                        characterCustomisation.Save(context);

                    deletedCharacterAppearances.Clear();
                    deletedCharacterBones.Clear();
                    deletedCharacterCustomisations.Clear();

                    foreach (Appearance characterAppearance in characterAppearances.Values)
                        characterAppearance.Save(context);
                    foreach (Bone characterBone in characterBones)
                        characterBone.Save(context);
                    foreach (Customisation characterCustomisation in characterCustomisations.Values)
                        characterCustomisation.Save(context);
                }

                saveMask = PlayerSaveMask.None;
            }

            model.TimePlayedLevel = (uint)TimePlayedLevel;
            entity.Property(p => p.TimePlayedLevel).IsModified = true;
            model.TimePlayedTotal = (uint)TimePlayedTotal;
            entity.Property(p => p.TimePlayedTotal).IsModified = true;

            foreach (StatValue stat in stats.Values)
                stat.SaveCharacter(CharacterId, context);

            Inventory.Save(context);
            CurrencyManager.Save(context);
            PathManager.Save(context);
            TitleManager.Save(context);
            CostumeManager.Save(context);
            PetCustomisationManager.Save(context);
            KeybindingManager.Save(context);
            SpellManager.Save(context);
            DatacubeManager.Save(context);
            MailManager.Save(context);
            ZoneMapManager.Save(context);
            QuestManager.Save(context);
        }

        /// <summary>
        /// Returns the time in seconds that has past since the last <see cref="Player"/> save.
        /// </summary>
        public double GetTimeSinceLastSave()
        {
            return SaveDuration - saveTimer.Time;
        }
    }
}
