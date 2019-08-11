using System.Threading.Tasks;
using NexusForever.WorldServer.Command.Attributes;
using NexusForever.WorldServer.Command.Contexts;
using NexusForever.WorldServer.Game.Entity.Static;





namespace NexusForever.WorldServer.Command.Handler
{
    [Name("Character Boosts and Unlocks")]
    public class BoostCommandHandler : CommandCategory
    {
        public BoostCommandHandler()
            : base(true, "boost")
        {
        }

        [SubCommandHandler("level", "Boosts your character to level 50, restart client for it to take effect")]
        public Task LevelSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            // Bump chracter level to 50
            // Later: Use levelup and exp to boost rather than directly changing the Player.Level
            context.Session.Player.Level = 50;
            return Task.CompletedTask;
        }

        [SubCommandHandler("money", "Grants some character currencies")]
        public Task MoneySubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            // Adds to major player currencies
            // Mebe find a better way to loop thru CurrencyType.cs later
            context.Session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, 500000000);
            context.Session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Renown, 500000);
            context.Session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.ElderGems, 500000);
            context.Session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.CraftingVoucher, 500000);
            context.Session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Prestige, 500000);
            context.Session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Glory, 500000);

            return Task.CompletedTask;
        }

        [SubCommandHandler("all", "Level boost, currencies and unlock all dyes")]
        public Task AllSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            //Unlocks all dyes on account
            context.Session.GenericUnlockManager.UnlockAll(GenericUnlockType.Dye);

            context.Session.Player.Level = 50;

            context.Session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, 500000000);
            context.Session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Renown, 500000);
            context.Session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.ElderGems, 500000);
            context.Session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.CraftingVoucher, 500000);
            context.Session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Prestige, 500000);
            context.Session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Glory, 500000);

            return Task.CompletedTask;
        }


    }
}