using NexusForever.WorldServer.Database.Character;
using NexusForever.WorldServer.Database.Character.Model;
using NexusForever.WorldServer.Game.Contact.Static;
using NexusForever.WorldServer.Game.Entity.Static;
using NexusForever.WorldServer.Game.Social.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Model;
using NexusForever.WorldServer.Network.Message.Model.Shared;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContactEntry = NexusForever.WorldServer.Database.Character.Model.Contacts;

namespace NexusForever.WorldServer.Game.Contact
{
    partial class ContactManager
    {
        /// <summary>
        /// Checks to see if the target character can become a contact
        /// </summary>
        /// <param name="session">Session of a player making the request</param>
        /// <param name="receipientId">Character ID of the target player</param>
        /// <param name="type">Type of contact to check</param>
        /// <returns></returns>
        private static bool CanBeContact(WorldSession session, ulong receipientId, ContactType type)
        {
            Dictionary<ContactType, uint> maxTypeMap = new Dictionary<ContactType, uint>
            {
                { ContactType.Friend, maxFriends },
                { ContactType.Account, maxFriends },
                { ContactType.Ignore, maxIgnored },
                { ContactType.Rival, maxRivals }
            };
            Dictionary<ContactType, ContactResult> maxTypeResponseMap = new Dictionary<ContactType, ContactResult>
                {
                    { ContactType.Friend, ContactResult.MaxFriends },
                    { ContactType.Account, ContactResult.MaxFriends },
                    { ContactType.Ignore, ContactResult.MaxIgnored },
                    { ContactType.Rival, ContactResult.MaxRivals }
                };
            // Check player isn't capped for this Contact Type
            if (type != ContactType.FriendAndRival && contactsCache.Values.Where(c => c.OwnerId == session.Player.CharacterId && c.Type == type && !c.IsPendingDelete).ToList().Count > maxTypeMap[type])
            {
                SendContactsResult(session, maxTypeResponseMap[type]);
                return false;
            }
            else if (type == ContactType.FriendAndRival)
            {
                // Check both maximum counts are checked
                if (contactsCache.Values.Where(c => c.OwnerId == session.Player.CharacterId && c.Type == ContactType.Friend && !c.IsPendingDelete).ToList().Count > maxTypeMap[ContactType.Friend])
                {
                    SendContactsResult(session, maxTypeResponseMap[ContactType.Friend]);
                    return false;
                }
                else if (contactsCache.Values.Where(c => c.OwnerId == session.Player.CharacterId && c.Type == ContactType.Rival && !c.IsPendingDelete).ToList().Count > maxTypeMap[ContactType.Rival])
                {
                    SendContactsResult(session, maxTypeResponseMap[ContactType.Rival]);
                    return false;
                }
            }

            // Check recipient isn't already contact of requested type.
            if (contactsCache.Values.FirstOrDefault(c => c.OwnerId == session.Player.CharacterId && c.ContactId == receipientId && c.Type == type && !c.IsPendingAcceptance && !c.IsPendingDelete) != null)
            {
                Dictionary<ContactType, ContactResult> alreadyContactResponseMap = new Dictionary<ContactType, ContactResult>
                {
                    { ContactType.Friend, ContactResult.PlayerAlreadyFriend },
                    { ContactType.Account, ContactResult.PlayerAlreadyFriend },
                    { ContactType.Ignore, ContactResult.PlayerAlreadyIgnored },
                    { ContactType.Rival, ContactResult.PlayerAlreadyRival }
                };

                SendContactsResult(session, alreadyContactResponseMap[type]);
                return false;
            }

            // CHeck and make sure recipient doesn't have existing request
            if (type == ContactType.Friend || type == ContactType.FriendAndRival)
                if (contactsCache.Values.FirstOrDefault(c => c.OwnerId == session.Player.CharacterId && c.ContactId == receipientId && c.Type == type && c.IsPendingAcceptance && !c.IsPendingDelete) != null)
                {
                    SendContactsResult(session, ContactResult.PlayerQueuedRequests);
                    return false;
                }

            return true;
        }
    }
}