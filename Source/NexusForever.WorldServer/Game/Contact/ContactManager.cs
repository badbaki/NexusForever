using NexusForever.Shared.Game.Events;
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
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        // TODO: move this to the config file
        private const double SaveDuration = 60d;
        private static double timeToSave = SaveDuration;

        /// <summary>
        /// Maximum amount of time a contact request can sit for before expiring
        /// </summary>
        private static float maxRequestDurationInDays = 7f; // TODO: Move this to config

        /// <summary>
        /// Id to be assigned to the next created contact.
        /// </summary>
        public static ulong NextContactId => nextContactId++;
        private static ulong nextContactId;

        // TODO: Make maximum contact types a config option, and figure out actual number.
        private static uint maxRivals = 20;
        private static uint maxIgnored = 20;
        private static uint maxFriends = 50;

        /// <summary>
        /// Minimum Id for the contact entry; Required to prevent the Client from marking the contact as Temporary
        /// </summary>
        private static ulong temporaryMod = 281474976710656;

        private static ConcurrentDictionary</*contactId*/ ulong, Contact> contactsCache = new ConcurrentDictionary<ulong, Contact>();

        /// <summary>
        /// Initialise the manager and run the start up tasks.
        /// </summary>
        public static void Initialise()
        {
            // Note: This makes the first ID equal temporaryMod + 1.This is because the client needs a value with a minimum of 281474976710656 for the Contact ID otherwise it is flagged
            // as a temporary contact.
            // TODO: Fix this to also include temporary contacts?
            ulong maxDbId =  CharacterDatabase.GetNextContactId();
            nextContactId = maxDbId > temporaryMod ? maxDbId + 1ul : maxDbId + temporaryMod + 1ul;

            List<ContactEntry> contactEntries = CharacterDatabase.GetAllContacts();
            foreach (ContactEntry contactEntry in contactEntries)
                contactsCache.TryAdd(contactEntry.Id, new Contact(contactEntry));

            log.Info($"Initialised {contactsCache.Count} contacts");
        }

        /// <summary>
        /// Called in the main update method. Used to run tasks to sync <see cref="Contact"/>to database.
        /// </summary>
        /// <param name="lastTick"></param>
        public static void Update(double lastTick)
        {
            timeToSave -= lastTick;
            if (timeToSave <= 0d)
            {
                var tasks = new List<Task>();

                foreach (Contact contact in contactsCache.Values)
                    tasks.Add(CharacterDatabase.SaveContact(contact));

                Task.WaitAll(tasks.ToArray());

                timeToSave = SaveDuration;
            }
        }
        
        /// <summary>
        /// Remove the provided <see cref="Contact"/> from the cache. Called by the <see cref="Contact"/> instance to remove itself after deletion.
        /// </summary>
        /// <param name="contactToRemove">Contact to be removed.</param>
        public static void RemoveFromCache(Contact contactToRemove)
        {
            contactsCache.Remove(contactToRemove.Id, out Contact removedContact);
        }

        /// <summary>
        /// Create a <see cref="Contact"/> request with another character
        /// </summary>
        /// <param name="session">Session making the request</param>
        /// <param name="recipientId">Character ID of the requested character</param>
        /// <param name="requestType">Type of Contact request to be made</param>
        /// <param name="message">Message to send to the recipient</param>
        public static void CreateFriendRequest(WorldSession session, ulong recipientId, string message)
        {
            ContactType requestType = ContactType.Friend;
            // Check rules.
            if (CanBeContact(session, recipientId, requestType))
            {
                // Respond to Requester that request was sent.
                SendContactsResult(session, ContactResult.RequestSent);

                GetExistingContact(session, recipientId, out Contact existingContact);
                if (existingContact != null)
                {
                    if (existingContact.Type == ContactType.Rival || existingContact.Type == ContactType.Ignore)
                    {
                        existingContact.MakePendingAcceptance();
                        existingContact.InviteMessage = message;
                        existingContact.RequestTime = DateTime.Now;
                    }
                }
                else
                {
                    // Save Pending Request.
                    Contact contactRequest = new Contact(NextContactId, session.Player.CharacterId, recipientId, message, requestType, true);
                    contactsCache.TryAdd(contactRequest.Id, contactRequest);
                }

                // Process Pending Request if user is online
                WorldSession targetSession = Shared.Network.NetworkManager<WorldSession>.Instance.GetSession(s => s.Player?.CharacterId == recipientId);
                if (targetSession != null)
                    SendPendingRequests(targetSession);
            }
        }

        /// <summary>
        /// Accept a <see cref="Contact"/> request
        /// </summary>
        /// <param name="session">Session of the player accepting the request</param>
        /// <param name="contactId">Contact.ID of the accepted request</param>
        /// <param name="returnRequest">True if the player is making the requester a friend as well</param>
        public static void AcceptFriendRequest(WorldSession session, ulong contactId, bool returnRequest = false)
        {
            if(!contactsCache.TryGetValue(contactId, out Contact contactRequest))
                throw new Exception($"Contact Request with ID {contactId} not found");

            // Ensure Contact can be accepted
            if (contactRequest.IsPendingDelete || !contactRequest.IsPendingAcceptance)
            {
                SendContactsResult(session, ContactResult.UnableToProcess);
                SendContactRequestRemove(session, contactRequest.Id);
                return;
            }

            // Change request to correct type and accept
            if(contactRequest.Type == ContactType.Friend)
                contactRequest.AcceptRequest();
            else if(contactRequest.Type == ContactType.Rival)
            {
                contactRequest.AcceptRequest();
                ChangeType(contactRequest, ContactType.FriendAndRival, null, true);
            }
            else if(contactRequest.Type == ContactType.Ignore)
            {
                contactRequest.AcceptRequest();
                ChangeType(contactRequest, ContactType.Friend, null, true);
            }
                
            // Remove the pending request from the receipient
            SendContactRequestRemove(session, contactRequest.Id);

            if (returnRequest)
            {
                // Handle receipient's request to the owner, if one exists
                GetExistingContact(session, contactRequest.OwnerId, out Contact existingRequest);
                if (existingRequest != null)
                {
                    existingRequest.AcceptRequest();
                    ChangeType(existingRequest, ContactType.Friend, null, true);
                    SendNewContact(session, GetContactData(existingRequest));
                    TryRemoveRequestFromOnlineUser(existingRequest);
                }
                else if (CanBeContact(session, contactRequest.OwnerId, contactRequest.Type))
                    ForceCreateContact(session, contactRequest.OwnerId, contactRequest.Type);
            }

            // Process Contact Request if user is online
            WorldSession targetSession = Shared.Network.NetworkManager<WorldSession>.Instance.GetSession(s => s.Player?.CharacterId == contactRequest.OwnerId);
            if (targetSession != null)
            {
                SendNewContact(targetSession, GetContactData(contactRequest));
                UpdateWatchers(targetSession);
            }

            // Notify new friend that this user has come online
            UpdateWatchers(session);
        }

        /// <summary>
        /// Decline a <see cref="Contact"/> request
        /// </summary>
        /// <param name="session">Session of the player declining the request</param>
        /// <param name="contactId">Contact.ID of the declined request</param>
        /// <param name="addIgnore">Flag to create an ignore entry for this user</param>
        public static void DeclineFriendRequest(WorldSession session, ulong contactId, bool addIgnore = false)
        {
            if(!contactsCache.TryGetValue(contactId, out Contact contactRequest))
                throw new Exception($"Contact Request with ID {contactId} not found");

            contactRequest.DeclineRequest();

            SendContactRequestRemove(session, contactId);

            if(addIgnore)
                CreateIgnored(session, contactRequest.OwnerId);
        }

        /// <summary>
        /// Create a <see cref="Contact"/> and force it to an accepted state
        /// </summary>
        /// <param name="session">Session of the player who will be the Owner of the contact</param>
        /// <param name="recipientId">Character ID that will be the contact</param>
        /// <param name="type">Type of contact</param>
        public static void ForceCreateContact(WorldSession session, ulong recipientId, ContactType type)
        {
            Contact newContact = new Contact(NextContactId, session.Player.CharacterId, recipientId, "", type, false);
            contactsCache.TryAdd(newContact.Id, newContact);
            
            SendNewContact(session, GetContactData(newContact));
        }

        /// <summary>
        /// Notifies online <see cref="Contact"/> owners, that the player has logged in or out.
        /// </summary>
        /// <param name="session">Player logging in or out</param>
        /// <param name="loggingOut">True if the user is logging out</param>
        public static void UpdateWatchers(WorldSession session, bool loggingOut = false)
        {
            List<Contact> playerContacts = GetWatcherList(session);

            foreach (Contact contact in playerContacts)
            {
                WorldSession contactSession = Shared.Network.NetworkManager<WorldSession>.Instance.GetSession(s => s.Player?.CharacterId == contact.OwnerId);
                if (contactSession != null)
                    contactSession.EnqueueMessageEncrypted(new ServerContactsUpdateStatus
                    {
                        PlayerIdentity = new TargetPlayerIdentity
                        {
                            RealmId = WorldServer.RealmId,
                            CharacterId = session.Player.CharacterId
                        },
                        LastOnlineInDays = loggingOut ? 0.00069f : 0
                    });
            }
        }

        /// <summary>
        /// Get list of associated <see cref="Contact"/> owners
        /// </summary>
        /// <param name="session">Player to look up associated contacts of</param>
        /// <returns></returns>
        private static List<Contact> GetWatcherList(WorldSession session)
        {
            List<Contact> contactList = contactsCache.Values.Where(d => d.ContactId == session.Player.CharacterId && d.IsPendingAcceptance == false && (d.Type == ContactType.Friend || d.Type == ContactType.FriendAndRival)).ToList();

            return contactList;
        }

        /// <summary>
        /// Remove a pending <see cref="Contact"/> request from an online player
        /// </summary>
        /// <param name="contactRequest">Contact request to remove</param>
        public static void TryRemoveRequestFromOnlineUser(Contact contactRequest)
        {
            WorldSession targetSession = Shared.Network.NetworkManager<WorldSession>.Instance.GetSession(s => s.Player?.CharacterId == contactRequest.ContactId);
            if (targetSession != null)
                SendContactRequestRemove(targetSession, contactRequest.Id);
        }

        /// <summary>
        /// Delete <see cref="Contact"/> from a player's contacts
        /// </summary>
        /// <param name="session">Session of player making the deletion</param>
        /// <param name="playerIdentity"><see cref="TargetPlayerIdentity"/> of the player to delete</param>
        /// <param name="type">Type to confirm deletion with</param>
        public static void DeleteContact(WorldSession session, TargetPlayerIdentity playerIdentity, ContactType type)
        {
            Contact contactToDelete = contactsCache.Values.FirstOrDefault(s => s.OwnerId == session.Player.CharacterId && s.ContactId == playerIdentity.CharacterId && !s.IsPendingDelete);
            if (contactToDelete == null)
                throw new Exception($"Contact matching realm {playerIdentity.RealmId} & characterId {playerIdentity.CharacterId} not found.");

            DeleteContact(session, contactToDelete, type);
        }

        /// <summary>
        /// Delete <see cref="Contact"/> from a player's contacts
        /// </summary>
        /// <param name="session">Session of a player making the deletion</param>
        /// <param name="contactToDelete">Contact to be deleted</param>
        public static void DeleteContact(WorldSession session, Contact contactToDelete, ContactType requestedTypeToDelete)
        {
            switch (requestedTypeToDelete)
            {
                case ContactType.Friend:
                    // TODO: Confirm this scenario
                    if (contactToDelete.IsPendingAcceptance)
                    {
                        contactToDelete.DeclineRequest();
                        TryRemoveRequestFromOnlineUser(contactToDelete);
                    }

                    if (contactToDelete.Type == ContactType.FriendAndRival)
                    {   
                        ChangeType(contactToDelete, ContactType.Rival, session, true);
                        return;
                    }
                    else
                        contactToDelete.EnqueueDelete();
                    break;
                case ContactType.Rival:
                    if (contactToDelete.Type == ContactType.FriendAndRival)
                    {
                        ChangeType(contactToDelete, ContactType.Friend, session, true);
                        return;
                    }
                    else
                        contactToDelete.EnqueueDelete();
                    break;
                case ContactType.Ignore:
                    if (contactToDelete.IsPendingAcceptance)
                        ChangeType(contactToDelete, ContactType.Friend, session, true);
                    else
                        contactToDelete.EnqueueDelete();
                    session.Player.IgnoreList.Remove(contactToDelete.ContactId);
                    break;
            }

            SendContactDelete(session, contactToDelete.Id);
        }

        /// <summary>
        /// Set a private note associated with a <see cref="Contact"/>
        /// </summary>
        /// <param name="session">Sesion of the player making the note change</param>
        /// <param name="playerIdentity">CharacterIdentity of the player's note to update</param>
        /// <param name="note">Note to set</param>
        public static void SetPrivateNote(WorldSession session, TargetPlayerIdentity playerIdentity, string note)
        {
            Contact contactToModify = contactsCache.Values.FirstOrDefault(s => s.OwnerId == session.Player.CharacterId && s.ContactId == playerIdentity.CharacterId && !s.IsPendingDelete);
            if (contactToModify == null)
                throw new Exception($"Contact matching realm {playerIdentity.RealmId} & characterId {playerIdentity.CharacterId} not found.");

            SetPrivateNote(session, contactToModify, note);
        }

        /// <summary>
        /// Set a private note associated with a <see cref="Contact"/>
        /// </summary>
        /// <param name="session"></param>
        /// <param name="contactToModify"></param>
        /// <param name="Note"></param>
        public static void SetPrivateNote(WorldSession session, Contact contactToModify, string Note)
        {
            contactToModify.PrivateNote = Note;

            session.EnqueueMessageEncrypted(new ServerContactsSetNote
            {
                ContactId = contactToModify.Id,
                Note = contactToModify.PrivateNote
            });
        }

        /// <summary>
        /// Create a rival <see cref="Contact"/>
        /// </summary>
        /// <param name="session">Session of a player making the rival request</param>
        /// <param name="recipientId">Character ID of the player to make a rival of</param>
        public static void CreateRival(WorldSession session, ulong recipientId)
        {
            if (CanBeContact(session, recipientId, ContactType.Rival))
            {
                GetExistingContact(session, recipientId, out Contact existingContact);
                if (existingContact != null)
                {
                    ChangeType(existingContact, ContactType.Rival, session);
                }
                else
                    ForceCreateContact(session, recipientId, ContactType.Rival);
            }
        }

        /// <summary>
        /// Create an ignored <see cref="Contact"/>
        /// </summary>
        /// <param name="session">Session of a player making the ignore request</param>
        /// <param name="recipientId">Character ID of the player to ignore</param>
        public static void CreateIgnored(WorldSession session, ulong recipientId)
        {
            if (CanBeContact(session, recipientId, ContactType.Ignore))
            {
                GetExistingContact(session, recipientId, out Contact existingContact);
                if (existingContact != null)
                {
                    if(!existingContact.IsPendingAcceptance)
                    {
                        ChangeType(existingContact, ContactType.Ignore, session);
                        session.Player.IgnoreList.Add(existingContact.ContactId);
                        return;
                    }
                    else
                    {
                        DeleteContact(session, existingContact, existingContact.Type);

                        if (existingContact.IsPendingAcceptance)
                        {
                            TryRemoveRequestFromOnlineUser(existingContact);
                        }
                    }
                }

                ForceCreateContact(session, recipientId, ContactType.Ignore);
                session.Player.IgnoreList.Add(recipientId);
                SendContactsResult(session, ContactResult.PlayerOnIgnored);
            }
        }

        /// <summary>
        /// Return a <see cref="Contact"/>, if there is one, that has the same owner and target player, but of a different <see cref="ContactType"/>
        /// </summary>
        /// <param name="session">Session of a player making the request</param>
        /// <param name="recipientId">Character ID of the target player</param>
        /// <param name="type">Type of contact not to match</param>
        /// <returns></returns>
        private static void GetExistingContact(WorldSession session, ulong recipientId, out Contact contact)
        {
            contact = contactsCache.Values.FirstOrDefault(c => c.OwnerId == session.Player.CharacterId && c.ContactId == recipientId && !c.IsPendingDelete);
        }

        private static void ChangeType(Contact contact, ContactType newType, WorldSession session = null, bool forceNewType = false)
        {
            if (forceNewType)
                contact.Type = newType;
            else
                switch (contact.Type)
                {
                    case ContactType.Rival:
                        if (newType == ContactType.Friend)
                            contact.Type = ContactType.FriendAndRival;
                        else if (newType == ContactType.Ignore)
                            contact.Type = ContactType.Ignore;
                        break;
                    case ContactType.Friend:
                        if (newType == ContactType.Rival)
                            contact.Type = ContactType.FriendAndRival;
                        else if (newType == ContactType.Ignore)
                            contact.Type = ContactType.Ignore;
                        break;
                    case ContactType.Ignore:
                        if (newType == ContactType.Rival)
                            contact.Type = ContactType.Rival;
                        else
                            contact.Type = ContactType.Ignore;
                        break;
                    case ContactType.FriendAndRival:
                        contact.Type = newType;
                        break;
                }

            if (session != null)
                SendContactsUpdateType(session, contact);
            else
            {
                session = Shared.Network.NetworkManager<WorldSession>.Instance.GetSession(s => s.Player?.CharacterId == contact.OwnerId);
                if(session != null)
                    SendContactsUpdateType(session, contact);
            }
        }

        private static void SendContactsUpdateType(WorldSession session, Contact contact)
        {
            
            session.EnqueueMessageEncrypted(new ServerContactsUpdateType
            {
                ContactId = contact.Id,
                Type = contact.Type
            });
        }

        /// <summary>
        /// Called by a <see cref="Player"/> when logging in
        /// </summary>
        /// <param name="session">Session of the player logging in</param>
        public static void OnLogin(WorldSession session)
        {
            SendPersonalStatus(session);
            SendAccountStatus(session);
            SendContactsListForPlayer(session);
            SendAccountList(session);
            SendPendingRequests(session);

            UpdateWatchers(session);
        }

        /// <summary>
        /// Called by a <see cref="Player"/> when logging out
        /// </summary>
        /// <param name="session">Session of the player logging out</param>
        public static void OnLogout(WorldSession session)
        {
            UpdateWatchers(session, true);
        }

        public static List<ulong> GetIgnoreList(Character character)
        {
            return contactsCache.Values.Where(c => c.OwnerId == character.Id && c.Type == ContactType.Ignore).Select(v => v.ContactId).ToList();
        }

        /// <summary>
        /// Sends pending <see cref="Contact"/> requests to the player
        /// </summary>
        /// <param name="session">Session of a player </param>
        private static void SendPendingRequests(WorldSession session)
        {
            List<ServerContactsRequestList.RequestData> contactRequestDataList = new List<ServerContactsRequestList.RequestData>();

            List<Contact> contactRequestsToSend = new List<Contact>();

            foreach (Contact contactRequest in contactsCache.Values.Where(s => s.ContactId == session.Player.CharacterId && s.IsPendingAcceptance && !s.IsPendingDelete).ToList())
            {
                CalculateExpiryTime(contactRequest, out float expiryTime);
                if (expiryTime > 0f)
                    contactRequestsToSend.Add(contactRequest);
                else
                    contactRequest.DeclineRequest();
            }

            session.EnqueueEvent(new TaskGenericEvent<List<Character>>(CharacterDatabase.GetCharactersByIdList(contactRequestsToSend.Select(i => i.OwnerId).ToList()),
                characters =>
            {
                foreach (Character character in characters)
                {
                    Contact contactRequest = contactRequestsToSend.FirstOrDefault(i => i.OwnerId == character.Id);
                    if(contactRequest != null)
                    {
                        CalculateExpiryTime(contactRequest, out float expiryTime);
                        contactRequestDataList.Add(GetContactRequestData(contactRequest, character, contactRequest.InviteMessage, expiryTime));
                    }
                }

                session.EnqueueMessageEncrypted(new ServerContactsRequestList
                {
                    ContactRequests = contactRequestDataList
                });
            }));
        }

        /// <summary>
        /// Create a <see cref="ServerContactsRequestList.RequestData"/> for a <see cref="Character"/>
        /// </summary>
        /// <param name="contactId">ID of the Contact entry</param>
        /// <param name="character">Character to build the request of</param>
        /// <param name="message">Message to be sent to with the request</param>
        /// <returns></returns>
        private static ServerContactsRequestList.RequestData GetContactRequestData(Contact contactRequest, Character character, string message, float expiryTime)
        {
            return new ServerContactsRequestList.RequestData
            {
                ContactId = contactRequest.Id,
                PlayerIdentity = new TargetPlayerIdentity
                {
                    RealmId = WorldServer.RealmId,
                    CharacterId = character.Id
                },
                ContactType = ContactType.Friend,
                Message = message,
                Name = character.Name,
                Class = (Class)character.Class,
                Path = (Path)character.ActivePath,
                Level = character.Level,
                ExpiryInDays = expiryTime // TODO: Calculate expiry date and delete record if request expires. Currently set to 1 week.
            };
        }

        /// <summary>
        /// Calculate the expiry time, as a float, for this <see cref="Contact"/> request
        /// </summary>
        /// <param name="contact">The contact to calculate the expiry for</param>
        /// <param name="expiryTime">The expiry time will be returned via the out parameter</param>
        private static void CalculateExpiryTime(Contact contact, out float expiryTime)
        {
            expiryTime = maxRequestDurationInDays - (float)Math.Abs(contact.RequestTime.Subtract(DateTime.Now).TotalDays);
        }

        /// <summary>
        /// Set a player's <see cref="ChatPresenceState"/>
        /// </summary>
        /// <param name="session">Session of a player to send the update to</param>
        private static void SendPersonalStatus(WorldSession session)
        {
            session.EnqueueMessageEncrypted(new ServerContactsSetPresence
            {
                AccountId = session.Account.Id,
                Presence = ChatPresenceState.Available
            });
        }

        /// <summary>
        /// Send a player's <see cref="Contact"/> list to them
        /// </summary>
        /// <param name="session">Session of a player to receive the list</param>
        private static void SendContactsListForPlayer(WorldSession session)
        {
            List<Contact> contactList = contactsCache.Values.Where(d => d.OwnerId == session.Player.CharacterId && !d.IsPendingDelete).ToList();

            List<ContactData> contactDataList = BuildContactsListData(contactList);
            SendContactsList(session, contactDataList);
        }

        /// <summary>
        /// Creates a list of <see cref="ContactData"/> from a list of <see cref="Contact"/> to be used as part of a player's contact list
        /// </summary>
        /// <param name="contactList">List of Contacts to be converted</param>
        /// <returns></returns>
        private static List<ContactData> BuildContactsListData(List<Contact> contactList)
        {
            List<ContactData> contactsList = new List<ContactData>();

            foreach (Contact contact in contactList)
                if(!(contact.Type == ContactType.Friend && contact.IsPendingAcceptance))
                    contactsList.Add(GetContactData(contact));

            return contactsList;
        }

        /// <summary>
        /// Create a <see cref="ContactData"/> from a <see cref="Contact"/>
        /// </summary>
        /// <param name="contact">Contact to build a ContactData from</param>
        /// <returns></returns>
        private static ContactData GetContactData(Contact contact)
        {
            return new ContactData
            {
                ContactId = contact.Id,
                PlayerIdentity = new TargetPlayerIdentity
                {
                    RealmId = WorldServer.RealmId,
                    CharacterId = contact.ContactId
                },
                Note = contact.PrivateNote,
                Type = contact.Type
            };
        }

        /// <summary>
        /// Send the <see cref="ServerContactsList"/> packet to a player
        /// </summary>
        /// <param name="session">Session of a player to receive the list</param>
        /// <param name="contacts">List of contacts to be included</param>
        private static void SendContactsList(WorldSession session, List<ContactData> contacts)
        {
            var ContactsList = new ServerContactsList();
            ContactsList.Contacts = contacts;
            session.EnqueueMessageEncrypted(ContactsList);
        }

        /// <summary>
        /// Send the <see cref="ServerContactsAccountStatus"/> packet to a player
        /// </summary>
        /// <param name="session">Session of a player to receive the packet</param>
        private static void SendAccountStatus(WorldSession session)
        {
            session.EnqueueMessageEncrypted(new ServerContactsAccountStatus
            {
                AccountPublicStatus = "",
                AccountNickname = "",
                Presence = ChatPresenceState.Available,
                BlockStrangerRequests = true
            });
        }

        /// <summary>
        /// Send a list of account friends to a player
        /// </summary>
        /// <param name="session">Session of a player recieving the list</param>
        private static void SendAccountList(WorldSession session)
        {
            var serverFriendshipAccountList = new ServerContactsAccountList();
            session.EnqueueMessageEncrypted(serverFriendshipAccountList);
        }

        /// <summary>
        /// Send a Contacts Result packet to a player
        /// </summary>
        /// <param name="session">Session of a player receiving the results packet</param>
        /// <param name="result">Result code to be used</param>
        public static void SendContactsResult(WorldSession session, ContactResult result)
        {
            session.EnqueueMessageEncrypted(new ServerContactsRequestResult
            {
                Unknown0 = "",
                Results = result
            });
        }

        /// <summary>
        /// Send a new <see cref="Contact"/> to a player
        /// </summary>
        /// <param name="session">Session to send the contact to</param>
        /// <param name="contactData">ContactData to be sent</param>
        private static void SendNewContact(WorldSession session, ContactData contactData)
        {
            session.EnqueueMessageEncrypted(new ServerContactsAdd
            {
                Contact = contactData
            });
        }

        /// <summary>
        /// Send a delete packet for a <see cref="Contact"/> to a player
        /// </summary>
        /// <param name="session">Session to send the contact delete packet to</param>
        /// <param name="contactId">Contact ID to be deleted</param>
        private static void SendContactDelete(WorldSession session, ulong contactId)
        {
            session.EnqueueMessageEncrypted(new ServerContactsDeleteResult
            {
                ContactId = contactId
            });
        }

        /// <summary>
        /// Send a contact request removal packet to a player
        /// </summary>
        /// <param name="session">Session to send the contact request removal packet to</param>
        /// <param name="contactId">Contact ID of the request to be removed</param>
        private static void SendContactRequestRemove(WorldSession session, ulong contactId)
        {
            session.EnqueueMessageEncrypted(new ServerContactsRequestRemove
            {
                ContactId = contactId
            });
        }
    }
}
