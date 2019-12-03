using NexusForever.Shared.Network;
using NexusForever.Shared.Network.Message;
using NexusForever.WorldServer.Game.Contact.Static;
using NexusForever.WorldServer.Network.Message.Model.Shared;

namespace NexusForever.WorldServer.Network.Message.Model
{
    [Message(GameMessageOpcode.ClientPlayerInfoRequest)]
    public class ClientPlayerInfoRequest : IReadable
    {
        public ContactType Type { get; private set; }
        public TargetPlayerIdentity Identity { get; } = new TargetPlayerIdentity();

        public void Read(GamePacketReader reader)
        {
            Type = (ContactType)reader.ReadByte(4u);
            Identity.Read(reader);
        }
    }
}
