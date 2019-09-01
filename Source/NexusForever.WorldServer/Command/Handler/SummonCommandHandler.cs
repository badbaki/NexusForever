using System.Threading.Tasks;
using NexusForever.WorldServer.Command.Attributes;
using NexusForever.WorldServer.Command.Contexts;
using NLog;
using NexusForever.WorldServer.Game.Entity;
using NexusForever.Shared.GameTable.Model;
using NexusForever.Shared.GameTable;
using System.Linq;

namespace NexusForever.WorldServer.Command.Handler
{
    [Name("Summon")]
    public class SummonCommandHandler : CommandCategory
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        public SummonCommandHandler()
            : base(true, "summon")
        {
        }

        //[SubCommandHandler("entity", "creature2Id - summons entity to the player's location")]
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

        [SubCommandHandler("disguise", "creature2Id - changes player disguise")]
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

        [SubCommandHandler("clear", "clears player disguise")]
        public Task ClearSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            //clear disguise
            context.Session.Player.ResetAppearance();
            return Task.CompletedTask;
        }
    }
}