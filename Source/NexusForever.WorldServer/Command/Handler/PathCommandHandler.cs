using System.Threading.Tasks;
using NexusForever.WorldServer.Command.Attributes;
using NexusForever.WorldServer.Command.Contexts;
using NexusForever.WorldServer.Game.Entity.Static;
using NexusForever.WorldServer.Network.Message.Handler;
using NLog;
using NexusForever.WorldServer.Game.Account.Static;

namespace NexusForever.WorldServer.Command.Handler
{
    [Name("Path", Permission.None)]
    public class PathCommandHandler : CommandCategory
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        public PathCommandHandler()
            : base(true, "path")
        {
        }

        [SubCommandHandler("activate", "pathId - Activate a path for this player.", Permission.CommandPathActivate)]
        public Task AddPathActivateSubCommand(CommandContext context, string command, string[] parameters)
        {
            if (parameters.Length < 1 || parameters.Length > 1)
            {
                context.SendMessageAsync($"invalid parameters. Ensure you pass the pathId you wish to activate.");
                return Task.CompletedTask;
            }

            if (uint.TryParse(parameters[0], out uint newPath))
            {
                if (newPath > 3)
                {
                    context.SendMessageAsync($"Path not recognised.");
                    return Task.CompletedTask;
                }

                PathHandler.HandlePathActivate(context.Session, new Network.Message.Model.ClientPathActivate
                {
                    Path = (Path)newPath,
                    UseTokens = true
                });
            }
            else
                context.SendMessageAsync($"Path not recognised.");

            return Task.CompletedTask;
        }

        [SubCommandHandler("unlock", "pathId - Unlock a path for this player.", Permission.CommandPathUnlock)]
        public Task AddPathUnlockSubCommand(CommandContext context, string command, string[] parameters)
        {
            if (parameters.Length < 1 || parameters.Length > 1)
            {
                context.SendMessageAsync($"invalid parameters. Ensure you pass the pathId you wish to unlock.");
                return Task.CompletedTask;
            }

            if (uint.TryParse(parameters[0], out uint unlockPath))
            {
                if (unlockPath > 3)
                {
                    context.SendMessageAsync($"Path not recognised.");
                    return Task.CompletedTask;
                }

                PathHandler.HandlePathUnlock(context.Session, new Network.Message.Model.ClientPathUnlock
                {
                    Path = (Path)unlockPath
                });
            }
            else
                context.SendMessageAsync($"Path not recognised.");

            return Task.CompletedTask;
        }

        [SubCommandHandler("addxp", "xp - Add the XP value to the player's curent Path XP.", Permission.CommandPathAddXp)]
        public Task AddPathAddXPSubCommand(CommandContext context, string command, string[] parameters)
        {
            if (parameters.Length < 1 || parameters.Length > 1)
            {
                context.SendMessageAsync($"invalid parameters. Ensure you pass the pathId you wish to unlock.");
                return Task.CompletedTask;
            }

            if (uint.TryParse(parameters[0], out uint xp) && xp > 0)
                context.Session.Player.PathManager.AddXp(xp);
            else
                context.SendMessageAsync($"XP value not recognised.");

            return Task.CompletedTask;
        }
    }
}
