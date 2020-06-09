using System.Threading.Tasks;
using NexusForever.WorldServer.Command.Attributes;
using NexusForever.WorldServer.Command.Contexts;
using NexusForever.WorldServer.Game.Account.Static;
using NexusForever.WorldServer.Game.Entity;
using NLog;

namespace NexusForever.WorldServer.Command.Handler
{
    [Name("Pet", Permission.None)]
    public class PetCommandHandler : CommandCategory
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        public PetCommandHandler()
            : base(true, "pet")
        {
        }

        [SubCommandHandler("unlockflair", "petFlairId - Unlock a pet flair", Permission.CommandPetUnlockFlair)]
        public Task AddFlairSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            if (parameters.Length != 1)
                return Task.CompletedTask;

            context.Session.Player.PetCustomisationManager.UnlockFlair(ushort.Parse(parameters[0]));
            return Task.CompletedTask;
        }

        [SubCommandHandler("summon", "creature2Id - summons Creature2 entity as pet", Permission.PRCommands)]
        public Task EntitySubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            if (parameters.Length != 1)
                return Task.CompletedTask;

            //clear current pet (hopefully all if many are summoned)
            uint? petTest = context.Session.Player.VanityPetGuid;
            if (petTest != null)
            {
                VanityPet oldVanityPet = context.Session.Player.GetVisible<VanityPet>(context.Session.Player.VanityPetGuid.Value);
                oldVanityPet?.RemoveFromMap();
                context.Session.Player.VanityPetGuid = null;
            }

            uint creatureId;
            uint.TryParse(parameters[0], out creatureId);

            log.Info($"Summoning entity {creatureId} to {context.Session.Player.Position}");

            var tempEntity = new VanityPet(context.Session.Player, creatureId);
            context.Session.Player.Map.EnqueueAdd(tempEntity, context.Session.Player.Position);
            return Task.CompletedTask;
        }

        [SubCommandHandler("clear", "clears player pet", Permission.PRCommands)]
        public Task ClearPetSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            //clear pet (hopefully all if many are summoned)
            VanityPet oldVanityPet = context.Session.Player.GetVisible<VanityPet>(context.Session.Player.VanityPetGuid.Value);
            oldVanityPet?.RemoveFromMap();
            context.Session.Player.VanityPetGuid = null;
            return Task.CompletedTask;
        }
    }
}
