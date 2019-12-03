using System;

namespace NexusForever.WorldServer.Database.Character.Model
{
    public partial class Contacts
    {
        public ulong Id { get; set; }
        public ulong OwnerId { get; set; }
        public ulong ContactId { get; set; }
        public byte Type { get; set; }
        public string InviteMessage { get; set; }
        public string PrivateNote { get; set; }
        public byte Accepted { get; set; }
        public DateTime RequestTime { get; set; }

        public virtual Character IdNavigation { get; set; }
    }
}