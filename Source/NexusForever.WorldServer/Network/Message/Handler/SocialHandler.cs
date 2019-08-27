using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Shared.GameTable;
using NexusForever.Shared.GameTable.Model;
using NexusForever.Shared.Network;
using NexusForever.Shared.Network.Message;
using NexusForever.WorldServer.Command;
using NexusForever.WorldServer.Game.Entity.Static;
using NexusForever.WorldServer.Game.Social;
using NexusForever.WorldServer.Network.Message.Model;
using NexusForever.WorldServer.Network.Message.Model.Shared;
using NLog;

namespace NexusForever.WorldServer.Network.Message.Handler
{
    public static class SocialHandler
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public static readonly string CommandPrefix = "!";

        [MessageHandler(GameMessageOpcode.ClientChat)]
        public static void HandleChat(WorldSession session, ClientChat chat)
        {
            if (chat.Message.StartsWith(CommandPrefix))
            {
                try
                {
                    IEnumerable<ChatFormat> chatLinks = SocialManager.ParseChatLinks(session, chat.Formats);
                    CommandManager.HandleCommand(session, chat.Message, true, chatLinks);
                    //CommandManager.ParseCommand(chat.Message, out string command, out string[] parameters);
                    //CommandHandlerDelegate handler = CommandManager.GetCommandHandler(command);
                    //handler?.Invoke(session, parameters);
                }
                catch (Exception e)
                {
                    log.Warn($"{e.Message}: {e.StackTrace}");
                }
            }
            else
                SocialManager.HandleClientChat(session, chat);
        }

        [MessageHandler(GameMessageOpcode.ClientEmote)]
        public static void HandleEmote(WorldSession session, ClientEmote emote)
        {
            StandState standState = StandState.Stand;
            if (emote.EmoteId != 0)
            {
                EmotesEntry entry = GameTableManager.Emotes.GetEntry(emote.EmoteId);
                if (entry == null)
                    throw (new InvalidPacketValueException("HandleEmote: Invalid EmoteId"));

                standState = (StandState)entry.StandState;
            }

            session.Player.EnqueueToVisible(new ServerEmote
            {
                Guid       = session.Player.Guid,
                StandState = standState,
                EmoteId    = emote.EmoteId
            });
        }

        [MessageHandler(GameMessageOpcode.ClientWhoRequest)]
        public static void HandleWhoRequest(WorldSession session, ClientWhoRequest request)
        {
            List<ServerWhoResponse.WhoPlayer> players = new List<ServerWhoResponse.WhoPlayer>();

            List<WorldSession> allSessions = NetworkManager<WorldSession>.GetSessions().ToList();
            foreach (WorldSession whoSession in allSessions)
            {
                if (whoSession.Player == null)
                    continue;

                if (whoSession.Player.IsLoading)
                    continue;

                if (whoSession.Player.Zone == null)
                    continue;

                players.Add(new ServerWhoResponse.WhoPlayer
                {
                    Name = whoSession.Player.Name,
                    Level = whoSession.Player.Level,
                    Race = whoSession.Player.Race,
                    Class = whoSession.Player.Class,
                    Path = whoSession.Player.Path,
                    Faction = whoSession.Player.Faction,
                    Sex = whoSession.Player.Sex,
                    Zone = whoSession.Player.Zone.Id
                });
            }

            session.EnqueueMessageEncrypted(new ServerWhoResponse
            {
                Players = players
            });
        }

        [MessageHandler(GameMessageOpcode.ClientChatWhisper)]
        public static void HandleWhisper(WorldSession session, ClientChatWhisper whisper)
        {
            SocialManager.HandleWhisperChat(session, whisper);
        }
    }
}
