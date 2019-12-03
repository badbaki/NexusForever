using NexusForever.Shared.Game.Events;
using NexusForever.Shared.Network.Message;
using NexusForever.WorldServer.Database.Character;
using NexusForever.WorldServer.Database.Character.Model;
using NexusForever.WorldServer.Game.Contact;
using NexusForever.WorldServer.Game.Contact.Static;
using NexusForever.WorldServer.Network.Message.Model;
using NLog;
using System;
using System.Collections.Generic;

namespace NexusForever.WorldServer.Network.Message.Handler
{
    public static class ContactsHandler
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        [MessageHandler(GameMessageOpcode.ClientContactsStatusChange)]
        public static void HandleStatusChange(WorldSession session, ClientContactsStatusChange request)
        {
            // Respond with 0x03AA
            session.EnqueueMessageEncrypted(new ServerContactsSetPresence
            {
                AccountId = session.Account.Id,
                Presence = request.Presence
            });
        }

        [MessageHandler(GameMessageOpcode.ClientContactsRequestAdd)]
        public static void HandleContactRequestAdd(WorldSession session, ClientContactsRequestAdd request)
        {
            if(request.PlayerName == session.Player.Name)
            {
                ContactManager.SendContactsResult(session, ContactResult.CannotInviteSelf);
                return;
            }

            session.EnqueueEvent(new TaskGenericEvent<Character>(CharacterDatabase.GetCharacterByName(request.PlayerName),
                character =>
            {
                if (character != null)
                {
                    // TODO: Handle Rival, Ignore, and Account Requests
                    if (request.Type == ContactType.Account)
                        ContactManager.SendContactsResult(session, ContactResult.UnableToProcess);
                    else
                        switch (request.Type)
                        {
                            case ContactType.Friend:
                                ContactManager.CreateFriendRequest(session, character.Id, request.Message);
                                break;
                            case ContactType.Rival:
                                ContactManager.CreateRival(session, character.Id);
                                break;
                            case ContactType.Ignore:
                                ContactManager.CreateIgnored(session, character.Id);
                                break;
                            default:
                                ContactManager.SendContactsResult(session, ContactResult.InvalidType);
                                break;
                        }

                    return;
                }
                else
                    ContactManager.SendContactsResult(session, ContactResult.PlayerNotFound);
            }));
        }

        [MessageHandler(GameMessageOpcode.ClientContactsRequestResponse)]
        public static void HandleRequestResponse(WorldSession session, ClientContactsRequestResponse request)
        {
            switch (request.Response)
            {
                case ContactResponse.Mutual:
                    ContactManager.AcceptFriendRequest(session, request.ContactId, true);
                    break;
                case ContactResponse.Accept:
                    ContactManager.AcceptFriendRequest(session, request.ContactId);
                    break;
                case ContactResponse.Decline:
                    ContactManager.DeclineFriendRequest(session, request.ContactId);
                    break;
                case ContactResponse.Ignore:
                    ContactManager.DeclineFriendRequest(session, request.ContactId, true);
                    break;
            }
        }

        [MessageHandler(GameMessageOpcode.ClientContactsRequestDelete)]
        public static void HandleDeleteResponse(WorldSession session, ClientContactsRequestDelete request)
        {
            ContactManager.DeleteContact(session, request.PlayerIdentity, request.Type);
        }

        [MessageHandler(GameMessageOpcode.ClientContactsSetNote)]
        public static void HandleModifyPrivateNote(WorldSession session, ClientContactsSetNote request)
        {
            ContactManager.SetPrivateNote(session, request.PlayerIdentity, request.Note);
        }

        //[MessageHandler(GameMessageOpcode.Client03BF)]
        //public static void Handle03BF(WorldSession session, Client03BF request)
        //{
        //    // Correct response according to parses. No idea what it does, currently.
        //    session.EnqueueMessageEncrypted(new Server03C0
        //    {
        //        Unknown0 = 1,
        //        Unknown1 = new byte[] { 21, 70, 14, 0, 0, 0, 0, 26 },
        //        Unknown2 = new byte[] { 18, 5, 0, 0 }
        //    });
        //}
    }
}
