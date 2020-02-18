using System.Collections.Generic;
using NexusForever.Shared.Network;
using NexusForever.Shared.Network.Message;

namespace NexusForever.WorldServer.Network.Message.Model
{
    [Message(GameMessageOpcode.ServerPublicEventStart)]
    public class ServerPublicEventStart : IWritable
    {
        public uint PublicEventId { get; set; }
        public bool Unknown0 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(PublicEventId);
            writer.Write(Unknown0);
        }
    }
}
