using NexusForever.Shared.Network;
using NexusForever.Shared.Network.Message;

namespace NexusForever.WorldServer.Network.Message.Model
{
    [Message(GameMessageOpcode.ServerPublicEventReason)]
    public class ServerPublicEventReason : IWritable
    {
        public ushort PublicEventId { get; set; } // 14
        public uint Reason { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(PublicEventId, 14u);
            writer.Write(Reason);
        }
    }
}
