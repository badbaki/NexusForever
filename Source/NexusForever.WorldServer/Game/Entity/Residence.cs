using NexusForever.WorldServer.Game.Entity.Network;
using NexusForever.WorldServer.Game.Entity.Network.Model;
using NexusForever.WorldServer.Game.Entity.Static;
using NexusForever.WorldServer.Network.Message.Model;

namespace NexusForever.WorldServer.Game.Entity
{
    [DatabaseEntity(EntityType.Residence)]
    public class Residence : WorldEntity
    {
        public Residence()
            : base(EntityType.Residence)
        {
            SetProperty(Property.BaseHealth, 101f);
            Position = new System.Numerics.Vector3(1471.32421875f, -714.194580078125f, 1440.951171875f);
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new ResidenceEntityModel
            {
                CreatureId = 6241
            };
        }

        public override ServerEntityCreate BuildCreatePacket()
        {
            ServerEntityCreate entityCreate = base.BuildCreatePacket();
            entityCreate.CreateFlags = 0;
            entityCreate.DisplayInfo = 21720;
            entityCreate.WorldPlacementData = new ServerEntityCreate.WorldPlacement
            {
                Type = 1,
                ActivePropId = 0,
                SocketId = 1159
            };
            return entityCreate;
        }
    }
}