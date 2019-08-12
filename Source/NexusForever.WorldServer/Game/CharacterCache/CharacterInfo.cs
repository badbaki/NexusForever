using NexusForever.WorldServer.Game.Entity.Static;
using System;
using CharacterModel = NexusForever.WorldServer.Database.Character.Model.Character;

namespace NexusForever.WorldServer.Game.CharacterCache
{
    public class CharacterInfo : ICharacter
    {
        public ulong CharacterId { get; private set; }
        public string Name { get; private set; }
        public Sex Sex { get; private set; }
        public Race Race { get; private set; }
        public Class Class { get; private set; }
        public Path Path { get; private set; }
        public uint Level { get; private set; }
        public Faction Faction { get; private set; }
        public DateTime LastOnline { get; private set; } = DateTime.Now;

        public CharacterInfo() { }

        public CharacterInfo(CharacterModel model)
        {
            CharacterId = model.Id;
            Name = model.Name;
            Sex = (Sex)model.Sex;
            Race = (Race)model.Race;
            Class = (Class)model.Class;
            Path = (Path)model.ActivePath;
            Level = model.Level;
            Faction = (Faction)model.FactionId;
            LastOnline = model.LastOnline;
        }

        public CharacterInfo(ICharacter model)
        {
            CharacterId = model.CharacterId;
            Name = model.Name;
            Sex = model.Sex;
            Race = model.Race;
            Class = model.Class;
            Path = model.Path;
            Level = model.Level;
            Faction = model.Faction;
        }

        public float GetOnlineStatus()
        {
            return (float)DateTime.Now.Subtract(LastOnline).TotalDays * -1f;
        }
    }
}