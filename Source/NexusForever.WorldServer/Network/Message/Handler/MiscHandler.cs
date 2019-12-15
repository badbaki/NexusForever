using System;
using System.Collections.Generic;
using NexusForever.Shared.Game.Events;
using NexusForever.Shared.Network;
using NexusForever.Shared.Network.Message;
using NexusForever.WorldServer.Database.Character;
using NexusForever.WorldServer.Database.Character.Model;
using NexusForever.WorldServer.Game.Account;
using NexusForever.WorldServer.Game.Account.Static;
using NexusForever.WorldServer.Game.CharacterCache;
using NexusForever.WorldServer.Game.Contact.Static;
using NexusForever.WorldServer.Game.Entity;
using NexusForever.WorldServer.Game.Entity.Static;
using NexusForever.WorldServer.Game.Map.Search;
using NexusForever.WorldServer.Game.Social;
using NexusForever.WorldServer.Network.Message.Model;
using NexusForever.WorldServer.Network.Message.Model.Shared;

namespace NexusForever.WorldServer.Network.Message.Handler
{
    public static class MiscHandler
    {
        private const float LocalChatDistace = 155f;

        [MessageHandler(GameMessageOpcode.ClientPing)]
        public static void HandlePing(WorldSession session, ClientPing ping)
        {
            session.Heartbeat.OnHeartbeat();
        }

        /// <summary>
        /// Handled responses to Player Info Requests.
        /// TODO: Put this in the right place, this is used by Mail & Contacts, at minimum. Probably used by Guilds, Circles, etc. too.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="request"></param>
        [MessageHandler(GameMessageOpcode.ClientPlayerInfoRequest)]
        public static void HandlePlayerInfoRequest(WorldSession session, ClientPlayerInfoRequest request)
        {
            ICharacter character = CharacterManager.Instance.GetCharacterInfo(request.Identity.CharacterId);
            if (character == null)
                throw new InvalidPacketValueException();

            if (request.Type == ContactType.Ignore) // Ignored user data request
                session.EnqueueMessageEncrypted(new ServerPlayerInfoBasicResponse
                {
                    ResultCode = 0,
                    Identity = new TargetPlayerIdentity
                    {
                        RealmId = WorldServer.RealmId,
                        CharacterId = character.CharacterId
                    },
                    Name = character.Name,
                    Faction = (Faction)character.Faction1,
                });
            else
                session.EnqueueMessageEncrypted(new ServerPlayerInfoFullResponse
                {
                    BaseData = new ServerPlayerInfoBasicResponse
                    {
                        ResultCode = 0,
                        Identity = new TargetPlayerIdentity
                        {
                            RealmId = WorldServer.RealmId,
                            CharacterId = character.CharacterId
                        },
                        Name = character.Name,
                        Faction = character.Faction1
                    },
                    IsClassPathSet = true,
                    Path = character.Path,
                    Class = character.Class,
                    Level = character.Level,
                    IsLastLoggedOnInDaysSet = true,
                    LastLoggedInDays = character.GetOnlineStatus()
                });

        }

        [MessageHandler(GameMessageOpcode.ClientToggleWeapons)]
        public static void HandleWeaponToggle(WorldSession session, ClientToggleWeapons toggleWeapons)
        {
            session.Player.Sheathed = toggleWeapons.ToggleState;
        }

        [MessageHandler(GameMessageOpcode.ClientRandomRollRequest)] //Fix Roll - BAKI
        public static void HandleRandomRoll(WorldSession session, ClientRandomRollRequest randomRoll)
        {
            if (randomRoll.MinRandom > randomRoll.MaxRandom)
                throw new InvalidPacketValueException();

            if (randomRoll.MaxRandom > 1000000u)
                throw new InvalidPacketValueException();
            int RandRollResult = new Random().Next((int)randomRoll.MinRandom, (int)randomRoll.MaxRandom);

            ServerChat rChat = new ServerChat
            {
                Guid = session.Player.Guid,
                Channel = ChatChannel.Emote,
                Text = $"(({session.Player.Name} rolls {RandRollResult} (1-{randomRoll.MaxRandom})))"
            };

            session.Player.Map.Search(
                session.Player.Position,
                LocalChatDistace,
                new SearchCheckRangePlayerOnly(session.Player.Position, LocalChatDistace, session.Player),
                out List<GridEntity> intersectedEntities
            );

            intersectedEntities.ForEach(e => ((Player)e).Session.EnqueueMessageEncrypted(rChat));

            session.EnqueueMessageEncrypted(new ServerRandomRollResponse
            {
                TargetPlayerIdentity = new TargetPlayerIdentity
                {
                    RealmId = WorldServer.RealmId,
                    CharacterId = session.Player.CharacterId
                },
                MinRandom = randomRoll.MinRandom,
                MaxRandom = randomRoll.MaxRandom,
                RandomRollResult = RandRollResult
            });
        }
    }
}
