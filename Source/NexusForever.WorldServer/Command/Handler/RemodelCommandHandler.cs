using System.Threading.Tasks;
using System.IO;
using NexusForever.Shared.GameTable;
using NexusForever.Shared.GameTable.Model;
using NexusForever.WorldServer.Command.Attributes;
using NexusForever.WorldServer.Command.Contexts;
using NexusForever.WorldServer.Game;
using NexusForever.WorldServer.Game.Housing;
using NexusForever.WorldServer.Game.Map;
using NexusForever.WorldServer.Network.Message.Handler;
using NexusForever.WorldServer.Network.Message.Model;
using NexusForever.WorldServer.Game.Housing.Static;

namespace NexusForever.WorldServer.Command.Handler
{
    [Name("Housing Sky and Ground Remodels")]
    public class RemodelCommandHandler : CommandCategory
    {
        public RemodelCommandHandler()
            : base(true, "remodel")
        {
        }

        [SubCommandHandler("sky", "set sky to specified SkyID")]
        public Task SkySubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            //remodel sky
            ClientHousingRemodel clientRemod = new ClientHousingRemodel();
            if (!(context.Session.Player.Map is ResidenceMap residenceMap))
            {
                context.SendMessageAsync("You need to be on a housing map to use this command!");
                return Task.CompletedTask;
            }
            Residence residence = ResidenceManager.GetResidence(context.Session.Player.Name).GetAwaiter().GetResult();
            residence.Sky = ushort.Parse(parameters[0]);

            residenceMap.Remodel(context.Session.Player, clientRemod);

            return Task.CompletedTask;
        }

        [SubCommandHandler("ground", "set ground to specified GroundID")]
        public Task GroundSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            //remodel ground
            ClientHousingRemodel clientRemod = new ClientHousingRemodel();
            if (!(context.Session.Player.Map is ResidenceMap residenceMap))
            {
                context.SendMessageAsync("You need to be on a housing map to use this command!");
                return Task.CompletedTask;
            }
            Residence residence = ResidenceManager.GetResidence(context.Session.Player.Name).GetAwaiter().GetResult();
            residence.Ground = ushort.Parse(parameters[0]);

            residenceMap.Remodel(context.Session.Player, clientRemod);

            return Task.CompletedTask;
        }

        [SubCommandHandler("clutter", "0 to turn clutter on, 1 to turn clutter off")]
        public Task ClutterSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            //change clutter
            ClientHousingRemodel clientRemod = new ClientHousingRemodel();
            if (!(context.Session.Player.Map is ResidenceMap residenceMap))
            {
                context.SendMessageAsync("You need to be on a housing map to use this command!");
                return Task.CompletedTask;
            }
            Residence residence = ResidenceManager.GetResidence(context.Session.Player.Name).GetAwaiter().GetResult();

            if (parameters[0] == "1")
            {
                residence.Flags = ResidenceFlags.groundClutterOff;
            }
            else if (parameters[0] == "0")
            {
                residence.Flags = 0;
            }

            residenceMap.Remodel(context.Session.Player, clientRemod);

            return Task.CompletedTask;
        }

    }
}