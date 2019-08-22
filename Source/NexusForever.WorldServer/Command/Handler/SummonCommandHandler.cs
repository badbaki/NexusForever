using System.Threading.Tasks;
using NexusForever.WorldServer.Command.Attributes;
using NexusForever.WorldServer.Command.Contexts;
using NLog;
using NexusForever.WorldServer.Game.Entity;

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

        [SubCommandHandler("entity", "creature2Id - summons entity to the player's location")]
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
    }
}