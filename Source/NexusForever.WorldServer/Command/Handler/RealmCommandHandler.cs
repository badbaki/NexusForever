using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NexusForever.Shared.Configuration;
using NexusForever.Shared.Database.Auth;
using NexusForever.Shared.Network;
using NexusForever.WorldServer.Command.Attributes;
using NexusForever.WorldServer.Command.Contexts;
using NexusForever.WorldServer.Game.Social;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Model.Shared;

namespace NexusForever.WorldServer.Command.Handler
{
    [Name("Realm Management")]
    public class RealmCommandHandler : CommandCategory
    {
        public RealmCommandHandler()
            : base(false, "realm", "server")
        {
        }

        [SubCommandHandler("motd", "message - Set the realm's Message of the Day and announce to the realm")]
        public async Task HandleMotd(CommandContext context, string subCommand, string[] parameters)
        {
            if (parameters.Length < 1)
            {
                await SendHelpAsync(context).ConfigureAwait(false);
                return;
            }

            ConfigurationManager<WorldServerConfiguration>.Instance.Config.MessageOfTheDay = string.Join(" ", parameters);
            ConfigurationManager<WorldServerConfiguration>.Instance.Save();

            string motd = ConfigurationManager<WorldServerConfiguration>.Instance.Config.MessageOfTheDay;
            foreach (WorldSession session in NetworkManager<WorldSession>.Instance.GetSessions())
                SocialManager.Instance.SendMessage(session, "MOTD: " + motd, channel: ChatChannel.Realm);

            await context.SendMessageAsync($"MOTD Updated!");
        }

        [SubCommandHandler("online", "Displays the users online")]
        [SubCommandHandler("o", "Displays the users online")]
        public async Task HandleOnlineCheck(CommandContext context, string subCommand, string[] parameters)
        {
            List<WorldSession> allSessions = NetworkManager<WorldSession>.Instance.GetSessions().ToList();

            int index = 0;
            foreach (WorldSession session in allSessions)
            {
                string infoString = "";
                infoString += $"[{index++}] Account {session.Account?.Email} ({session.Account?.Id}) connected";

                if (session.Player != null)
                    infoString += $" | Playing {session.Player?.Name}, {session.Player?.Level} {session.Player?.Race} {session.Player?.Class}";

                infoString += $" | Connected for {session.Uptime:%d}d {session.Uptime:%h}h {session.Uptime:%m}m";

                await context.SendMessageAsync(infoString);
            }

            if (allSessions.Count == 0)
                await context.SendMessageAsync($"No sessions connected.");

            await Task.CompletedTask;
        }

        [SubCommandHandler("uptime", "Displaye the current uptime of the server.")]
        public async Task HandleUptimeCheck(CommandContext context, string subCommand, string[] parameters)
        {
            await context.SendMessageAsync($"Currently up for {WorldServer.Uptime:%d}d {WorldServer.Uptime:%h}h {WorldServer.Uptime:%m}m {WorldServer.Uptime:%s}s");

            await Task.CompletedTask;
        }
    }
}