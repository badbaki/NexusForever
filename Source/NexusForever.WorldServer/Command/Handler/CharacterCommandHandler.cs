using System.Threading.Tasks;
using NexusForever.WorldServer.Command.Attributes;
using NexusForever.WorldServer.Command.Contexts;
using NexusForever.WorldServer.Game.Entity.Static;
using NexusForever.WorldServer.Game.Account.Static;

namespace NexusForever.WorldServer.Command.Handler
{
    [Name("Character", Permission.None)]
    public class CharacterCommandHandler : CommandCategory
    {

        public CharacterCommandHandler()
            : base(true, "character")
        {
        }

        [SubCommandHandler("addxp", "amount - Add the amount to your total xp.", Permission.None)]
        public Task AddXPCommand(CommandContext context, string command, string[] parameters)
        {
            if (parameters.Length > 0)
            {
                uint xp = uint.Parse(parameters[0]);

                if (context.Session.Player.Level < 50)
                    context.Session.Player.GrantXp(xp);
                else
                    context.SendMessageAsync("You must be less than max level.");
            }
            else
                context.SendMessageAsync("You must specify the amount of XP you wish to add.");

            return Task.CompletedTask;
        }

        // TODO: Update after "SetStat" packets are available.
        [SubCommandHandler("level", "value - Set your level to the value passed in", Permission.None)]
        public Task SetLevelCommand(CommandContext context, string command, string[] parameters)
        {
            if (parameters.Length > 0)
            {
                byte level = byte.Parse(parameters[0]);

                if (context.Session.Player.Level < level && level <= 50)
                {
                    context.Session.Player.SetLevel(level);
                    context.SendMessageAsync($"Success! You are now level {level}.");
                }
                else
                    context.SendMessageAsync("Level must be more than your current level and no higher than level 50.");
            }
            else
            {
                context.SendMessageAsync("You must specify the level value you wish to assign.");
            }

            return Task.CompletedTask;
        }

        [SubCommandHandler("properties", "[property] [amount] -- change a selection of character properties", Permission.PRCommands)]
        public Task PropertiesSubCommandHandler(CommandContext context, string command, string[] parameters)
        {
            if (parameters.Length != 2)
            {
                context.SendMessageAsync("Avaliable properties: speed, mountspeed, gravity, jumpheight");
            }
            else
            {
                if (parameters[0].ToLower() == "speed")
                {
                    //change movement speed
                    float fValue = 0;
                    bool result = float.TryParse(parameters[1], out fValue);
                    if (result == true)
                    {
                        if ((fValue >= -5) & (fValue <= 8))
                        {
                            context.Session.Player.SetProperty(Property.MoveSpeedMultiplier, fValue, fValue);
                        }
                        else
                        {
                            context.SendMessageAsync("Invalid value. Enter a speed value between -5 and 8.");
                        }
                    }
                    else
                    {
                        context.SendMessageAsync("Invalid value. Enter a speed value between -5 and 8.");
                    }
                }
                else if (parameters[0].ToLower() == "mountspeed")
                {
                    //change mount speed
                    float fValue = 0;
                    bool result = float.TryParse(parameters[1], out fValue);
                    if (result == true)
                    {
                        if ((fValue >= 0) & (fValue <= 5))
                        {
                            context.Session.Player.SetProperty(Property.MountSpeedMultiplier, fValue, fValue);
                        }
                        else
                        {
                            context.SendMessageAsync("Invalid value. Enter a mountspeed value between 0 and 5.");
                        }
                    }
                    else
                    {
                        context.SendMessageAsync("Invalid value. Enter a mountspeed value between 0 and 5.");
                    }
                }
                else if (parameters[0].ToLower() == "gravity")
                {
                    //change gravity
                    float fValue = 0;
                    bool result = float.TryParse(parameters[1], out fValue);
                    if (result == true)
                    {
                        if ((fValue >= 0.1) & (fValue <= 5))
                        {
                            context.Session.Player.SetProperty(Property.GravityMultiplier, fValue, fValue);
                        }
                        else
                        {
                            context.SendMessageAsync("Invalid value. Enter a mountspeed value between 0.1 and 5.");
                        }
                    }
                    else
                    {
                        context.SendMessageAsync("Invalid value. Enter a mountspeed value between 0.1 and 5.");
                    }
                }
                else if (parameters[0].ToLower() == "jumpheight")
                {
                    //change jump
                    float fValue = 0;
                    bool result = float.TryParse(parameters[1], out fValue);
                    if (result == true)
                    {
                        if ((fValue >= 0) & (fValue <= 50))
                        {
                            context.Session.Player.SetProperty(Property.JumpHeight, fValue, fValue);
                        }
                        else
                        {
                            context.SendMessageAsync("Invalid value. Enter a mountspeed value between 0 and 50.");
                        }
                    }
                    else
                    {
                        context.SendMessageAsync("Invalid value. Enter a mountspeed value between 0 and 50.");
                    }
                }

            }
            return Task.CompletedTask;
        }
    }
}