using System;
using System.Collections.Generic;

namespace NexusForever.WorldServer.Database.Character.Model
{
    public partial class GuildMember
    {
        public ulong Id { get; set; }
        public ulong CharacterId { get; set; }
        public byte Rank { get; set; }
        public string Note { get; set; }

        public virtual Guild IdNavigation { get; set; }
    }
}
