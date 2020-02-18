using NexusForever.Shared.Network;
using NexusForever.Shared.Network.Message;
using System.Collections.Generic;

namespace NexusForever.WorldServer.Network.Message.Model
{
    [Message(GameMessageOpcode.ServerPublicEventObjectives)]
    public class ServerPublicEventObjectives : IWritable
    {
        public class Objective : IWritable
        {
            public class Status : IWritable
            {
                public class VirtualItemDepotObjective : IWritable
                {
                    public ushort ItemId { get; set; } // 14
                    public uint Count { get; set; }

                    public void Write(GamePacketWriter writer)
                    {
                        writer.Write(ItemId, 14u);
                        writer.Write(Count);
                    }
                }

                public uint CurrentStatus { get; set; }
                public uint Count { get; set; }
                public uint DynamicMax { get; set; }
                public uint Percent { get; set; }
                public uint SpellResourceIdx { get; set; }

                public byte DataType { get; set; } // 3
                public byte Empty { get; set; }
                public uint CapturePointObjective { get; set; }
                public uint CapturePointDefendObjective { get; set; }
                public List<VirtualItemDepotObjective> VirtualItemDepotObjectives { get; set; }

                public void Write(GamePacketWriter writer)
                {
                    writer.Write(CurrentStatus);
                    writer.Write(Count);
                    writer.Write(DynamicMax);
                    writer.Write(Percent);
                    writer.Write(SpellResourceIdx);
                    writer.Write(DataType, 3u);

                    switch(DataType)
                    {
                        case 0:
                            writer.Write(Empty);
                            break;
                        case 1:
                            writer.Write(CapturePointObjective);
                            break;
                        case 2:
                            writer.Write(CapturePointDefendObjective);
                            break;
                        case 3:
                            writer.Write(VirtualItemDepotObjectives.Count, 32u);
                            VirtualItemDepotObjectives.ForEach(v => v.Write(writer));
                            break;
                    }
                }
            }

            public class UnknownStructure0 : IWritable
            {
                public ushort Unknown0 { get; set; } // 14
                public uint Unknown1 { get; set; } // 17

                public void Write(GamePacketWriter writer)
                {
                    writer.Write(Unknown0, 14u);
                    writer.Write(Unknown1, 17u);
                }
            }

            public ushort ObjectiveId { get; set; } // 15
            public Status ObjectiveStatus { get; set; }
            public bool Busy { get; set; }
            public uint ElapsedTimeMs { get; set; }
            public uint NotificationMode { get; set; }
            public List<uint> Locations { get; set; } = new List<uint>();
            public List<UnknownStructure0> UnknownStructure0s { get; set; } = new List<UnknownStructure0>();

            public void Write(GamePacketWriter writer)
            {
                writer.Write(ObjectiveId, 15u);
                ObjectiveStatus.Write(writer);
                writer.Write(Busy);
                writer.Write(ElapsedTimeMs);
                writer.Write(NotificationMode);

                writer.Write(Locations.Count, 32u);
                Locations.ForEach(i => writer.Write(i));

                writer.Write(UnknownStructure0s.Count, 32u);
                UnknownStructure0s.ForEach(i => i.Write(writer));
            }
        }

        public ushort PublicEventId { get; set; } // 14
        public ushort PublicEventTeamId { get; set; } // 14
        public uint ElapsedTimeMs { get; set; }
        public bool Busy { get; set; }
        public List<Objective> Objectives { get; set; } = new List<Objective>();
        public List<uint> EventLocations { get; set; } = new List<uint>();
        public uint RewardType { get; set; }
        public uint[] RewardThresholds { get; set; } = new uint[3];
        public ushort Unknown0 { get; set; } // 14

        public void Write(GamePacketWriter writer)
        {
            writer.Write(PublicEventId, 14u);
            writer.Write(PublicEventTeamId, 14u);
            writer.Write(ElapsedTimeMs);
            writer.Write(Busy);

            writer.Write(Objectives.Count, 32u);
            Objectives.ForEach(o => o.Write(writer));

            writer.Write(EventLocations.Count, 32u);
            EventLocations.ForEach(e => writer.Write(e));

            writer.Write(RewardType);
            for (uint i = 0u; i < RewardThresholds.Length; i++)
                writer.Write(RewardThresholds[i]);

            writer.Write(Unknown0, 14u);
        }
    }
}
