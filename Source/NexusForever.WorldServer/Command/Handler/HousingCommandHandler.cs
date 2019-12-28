using System.IO;
using System.Threading.Tasks;
using NexusForever.Shared.GameTable;
using NexusForever.Shared.GameTable.Model;
using NexusForever.WorldServer.Command.Attributes;
using NexusForever.WorldServer.Command.Contexts;
using NexusForever.WorldServer.Game;
using NexusForever.WorldServer.Game.Housing;
using NexusForever.WorldServer.Game.Map;
using NexusForever.WorldServer.Game.Account.Static;
using NexusForever.WorldServer.Network.Message.Model;
using NexusForever.WorldServer.Game.Housing.Static;

namespace NexusForever.WorldServer.Command.Handler
{
    [Name("Housing", Permission.None)]
    public class HousingCommandHandler : CommandCategory
    {
        public HousingCommandHandler()
            : base(true, "house")
        {
        }

        [SubCommandHandler("teleport", "[name] - Teleport to a residence, optionally specifying a character", Permission.CommandHouseTeleport)]
        public Task TeleportSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            string name = parameters.Length == 0 ? context.Session.Player.Name : string.Join(" ", parameters);

            Residence residence = ResidenceManager.Instance.GetResidence(name).GetAwaiter().GetResult();
            if (residence == null)
            {
                if (parameters.Length == 0)
                    residence = ResidenceManager.Instance.CreateResidence(context.Session.Player);
                else
                {
                    context.SendMessageAsync("A residence for that character doesn't exist!");
                    return Task.CompletedTask;
                }
            }

            ResidenceEntrance entrance = ResidenceManager.Instance.GetResidenceEntrance(residence);
            context.Session.Player.TeleportTo(entrance.Entry, entrance.Position, 0u, residence.Id);

            return Task.CompletedTask;
        }

        [SubCommandHandler("decoradd", "decorId [quantity] - Add decor by id to your crate, optionally specifying quantity", Permission.CommandHouseDecorAdd)]
        public Task DecorAddSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            if (parameters.Length < 1 && parameters.Length > 2)
                return Task.CompletedTask;

            if (!(context.Session.Player.Map is ResidenceMap residenceMap))
            {
                context.SendMessageAsync("You need to be on a housing map to use this command!");
                return Task.CompletedTask;
            }

            uint decorInfoId = uint.Parse(parameters[0]);
            uint quantity    = parameters.Length == 2 ? uint.Parse(parameters[1]) : 1u;

            HousingDecorInfoEntry entry = GameTableManager.Instance.HousingDecorInfo.GetEntry(decorInfoId);
            if (entry == null)
            {
                context.SendMessageAsync($"Invalid decor info id {decorInfoId}!");
                return Task.CompletedTask;
            }

            residenceMap.DecorCreate(entry, quantity);
            return Task.CompletedTask;
        }

        [SubCommandHandler("decorlookup", "name - Returns a list of decor ids that match the supplied name", Permission.CommandHouseDecorLookup)]
        public Task DecorLookupSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            if (parameters.Length != 1)
                return Task.CompletedTask;

            var sw = new StringWriter();
            sw.WriteLine("Decor Lookup Results:");

            TextTable tt = GameTableManager.Instance.GetTextTable(context.Language);
            foreach (HousingDecorInfoEntry decorEntry in
                SearchManager.Instance.Search<HousingDecorInfoEntry>(parameters[0], context.Language, e => e.LocalizedTextIdName, true))
            {
                string text = tt.GetEntry(decorEntry.LocalizedTextIdName);
                sw.WriteLine($"({decorEntry.Id}) {text}");
            }

            context.SendMessageAsync(sw.ToString());
            return Task.CompletedTask;
        }

        [SubCommandHandler("remodel", "remodelType remodelId - Change ground/sky or toggle clutter", Permission.None)] //BAKI
        public Task RemodelSubCommandHandler(CommandContext context, string command, string[] parameters)
        {

            //remodel
            ClientHousingRemodel clientRemod = new ClientHousingRemodel();
            if (!(context.Session.Player.Map is ResidenceMap residenceMap))
            {
                context.SendMessageAsync("You need to be on a housing map to use this command!");
                return Task.CompletedTask;
            }

            Residence residence = ResidenceManager.Instance.GetResidence(context.Session.Player.Name).GetAwaiter().GetResult();

            //probably could use some better checks/message when they enter a wrong value
            if (parameters[0].ToLower() == "clutter")
            {
                if (parameters[1].ToLower() == "off")
                {
                    residence.Flags = ResidenceFlags.groundClutterOff;
                }
                else if (parameters[1].ToLower() == "on")
                {
                    residence.Flags = 0;
                }
                else
                {
                    context.Session.Player.SendSystemMessage("Invalid argument! Try again.");
                }
            }
            else if (parameters[0].ToLower() == "ground")
            {
                residence.Ground = ushort.Parse(parameters[1]);
            }
            else if (parameters[0].ToLower() == "sky")
            {
                residence.Sky = ushort.Parse(parameters[1]);
            }
            else
            {
                context.Session.Player.SendSystemMessage("Invalid argument! Try again.");
            }

            residenceMap.Remodel(context.Session.Player, clientRemod);
            return Task.CompletedTask;
        }

        [SubCommandHandler("plug", "housePlugID", Permission.PRCommands)] //BAKI
        public Task PlugSubCommandHandler(CommandContext context, string command, string[] parameters)
        {

            //remodel
            ClientHousingRemodel clientRemod = new ClientHousingRemodel();
            if (!(context.Session.Player.Map is ResidenceMap residenceMap))
            {
                context.SendMessageAsync("You need to be on a housing map to use this command!");
                return Task.CompletedTask;
            }

            Residence residence = ResidenceManager.Instance.GetResidence(context.Session.Player.Name).GetAwaiter().GetResult();

            //probably could use some better checks/message when they enter a wrong value
           if (parameters[0] != "")
            {
                uint residenceId = residence.GetResidenceEntryForPlug(uint.Parse(parameters[0]));
                if (residenceId > 0)
                {
                    ClientHousingPlugUpdate CHPUpdate = new ClientHousingPlugUpdate
                    {
                        PlugItem = uint.Parse(parameters[0]),
                        PlotInfo = 165
                    };
                    HousingPlugItemEntry HPIEntry = new HousingPlugItemEntry
                    {
                        Id = uint.Parse(parameters[0])
                    };
                    residenceMap.SetHousePlug(context.Session.Player, CHPUpdate, HPIEntry);
                }
                else
                {
                    context.SendMessageAsync("Oops! That plug ID isn't valid. Try again!");
                }       
            }
            else
            {
                context.SendMessageAsync("Oops! That plug ID isn't valid. Try again!");
            }

            return Task.CompletedTask;
        }
    }
}
