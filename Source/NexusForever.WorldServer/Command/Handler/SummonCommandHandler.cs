using System.Threading.Tasks;
using NexusForever.WorldServer.Command.Attributes;
using NexusForever.WorldServer.Command.Contexts;
using NLog;
using NexusForever.WorldServer.Game.Entity;
using NexusForever.Shared.GameTable.Model;
using NexusForever.Shared.GameTable;
using System.Linq;
using NexusForever.WorldServer.Game.Account.Static;
using NexusForever.WorldServer.Game.Entity.Static;

namespace NexusForever.WorldServer.Command.Handler
{
    [Name("Summon", Permission.None)]
    public class SummonCommandHandler : CommandCategory
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        public SummonCommandHandler()
            : base(true, "summon")
        {
        }

        [SubCommandHandler("entity", "creature2Id - summons entity to the player's location", Permission.ModMe)]
        public Task EntitySubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            if (parameters.Length != 1)
                return Task.CompletedTask;

            uint creatureId;
            uint.TryParse(parameters[0], out creatureId);

            log.Info($"Summoning entity {creatureId} to {context.Session.Player.Position}");

            var tempEntity = new VanityPet(context.Session.Player, creatureId);
            context.Session.Player.Map.EnqueueAdd(tempEntity, context.Session.Player.Position);
            return Task.CompletedTask;
        }

        [SubCommandHandler("disguise", "creature2Id - changes player disguise", Permission.PRCommands)]
        public Task DisguiseSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            if (parameters.Length != 1)
                return Task.CompletedTask;

            uint creatureId;
            uint.TryParse(parameters[0], out creatureId);

            Creature2Entry creature2 = GameTableManager.Creature2.GetEntry(creatureId);
            if (creature2 == null)
                return Task.CompletedTask;

            Creature2DisplayGroupEntryEntry displayGroupEntry = GameTableManager.Creature2DisplayGroupEntry.Entries.FirstOrDefault(d => d.Creature2DisplayGroupId == creature2.Creature2DisplayGroupId);
            if (displayGroupEntry == null)
                return Task.CompletedTask;

            context.Session.Player.SetDisplayInfo(displayGroupEntry.Creature2DisplayInfoId);
            return Task.CompletedTask;
        }

        [SubCommandHandler("clear", "clears player disguise", Permission.PRCommands)]
        public Task ClearSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            //clear disguise
            context.Session.Player.ResetAppearance();
            return Task.CompletedTask;
        }

        [SubCommandHandler("clearPet", "clears player pet", Permission.PRCommands)]
        public Task ClearPetSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            //clear pet (hopefully all if many are summoned)
            VanityPet oldVanityPet = context.Session.Player.GetVisible<VanityPet>(context.Session.Player.VanityPetGuid.Value);
            oldVanityPet?.RemoveFromMap();
            context.Session.Player.VanityPetGuid = 0u;
            return Task.CompletedTask;
        }

        [SubCommandHandler("nitro", "Zoom zoom", Permission.BakiBreaks)]
        public Task NitroSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            if (parameters[0].ToLower() == "speed")
            {
                //change movement speed
                float changeTo = float.Parse(parameters[1]);
                context.Session.Player.SetProperty(Property.MoveSpeedMultiplier, changeTo, changeTo);
            }
            else if (parameters[0].ToLower() == "mspeed")
            {
                //change mount speed
                float changeTo = float.Parse(parameters[1]);
                context.Session.Player.SetProperty(Property.MountSpeedMultiplier, changeTo, changeTo);
            }
            else if (parameters[0].ToLower() == "gravity")
            {
                //change gravity
                float changeTo = float.Parse(parameters[1]);
                context.Session.Player.SetProperty(Property.GravityMultiplier, changeTo, changeTo);
            }
            else if (parameters[0].ToLower() == "jump")
            {
                //change jump
                float changeTo = float.Parse(parameters[1]);
                context.Session.Player.SetProperty(Property.JumpHeight, changeTo, changeTo);
            }
            return Task.CompletedTask;
        }
    }
}