using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NexusForever.Shared.GameTable;
using NexusForever.Shared.GameTable.Model;
using NexusForever.WorldServer.Command.Attributes;
using NexusForever.WorldServer.Command.Contexts;
using NexusForever.WorldServer.Game;
using NexusForever.WorldServer.Game.Social;
using NexusForever.WorldServer.Game.Social.Model;
using NexusForever.WorldServer.Network.Message.Model.Shared;
using NLog;

namespace NexusForever.WorldServer.Command.Handler
{
    [Name("Items")]
    public class ItemCommandHandler : CommandCategory
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        public ItemCommandHandler()
            : base(true, "item")
        {
        }

        [SubCommandHandler("add", "itemId [quantity] [charges] - Add an item to inventory, optionally specifying quantity and charges")]
        public Task AddItemSubCommand(CommandContext context, string command, string[] parameters)
        {
            if (parameters.Length <= 0)
                return context.SendMessageAsync($"Not enough parameters. Please try again.");

            List<ChatFormat> ItemLinks = context.ChatLinks.Where(i => (i.Type == Game.Social.Static.ChatFormatType.ItemItemId || i.Type == Game.Social.Static.ChatFormatType.ItemGuid || i.Type == Game.Social.Static.ChatFormatType.ItemFull)).ToList();

            uint itemId = 0;
            if (ItemLinks.Count == 1)
            {
                ChatFormat itemLink = ItemLinks[0];
                if (itemLink.Type == Game.Social.Static.ChatFormatType.ItemItemId)
                {
                    ChatFormatItemId formatModel = (ChatFormatItemId)itemLink.FormatModel;
                    itemId = formatModel.ItemId;
                }
                else if (itemLink.Type == Game.Social.Static.ChatFormatType.ItemGuid)
                {
                    ChatFormatItemGuid formatModel = (ChatFormatItemGuid)itemLink.FormatModel;
                    itemId = context.Session.Player.Inventory.GetItem(formatModel.Guid).Entry.Id;
                }
            }
            else if (ItemLinks.Count > 1)
                return context.SendMessageAsync($"Too many item links included. Please try again using a single item link.");
            else
            {
                if (uint.TryParse(parameters[0], out uint parsedItemId))
                    itemId = parsedItemId;
                else
                    return context.SendMessageAsync($"Failed to parse Item ID. Please ensure you entered it accurately, or try re-linking the item.");
            }

            uint amount = 1;
            if (parameters.Length > 1)
                amount = uint.Parse(parameters[1]);

            uint charges = 1;
            if (parameters.Length > 2)
                charges = uint.Parse(parameters[2]);

            if (itemId > 0)
                context.Session.Player.Inventory.ItemCreate(itemId, amount, Game.Entity.Static.ItemUpdateReason.Cheat, charges);
            else
                context.SendMessageAsync($"Problem trying to create item: {parameters[0]}. Please try again.");

            return Task.CompletedTask;
        }

        private IEnumerable<uint> GetTextIds(Item2Entry entry)
        {
            Item2Entry item = GameTableManager.Item.GetEntry(entry.Id);
            if (item != null && item.LocalizedTextIdName != 0)
                yield return item.LocalizedTextIdName;
        }

        [SubCommandHandler("lookup", "itemName - Search for an item by name.")]
        public Task FindItemSubCommand(CommandContext context, string command, string[] parameters)
        {
            if (parameters.Length <= 0)
            {
                context.SendMessageAsync($"Not enough parameters. Please try again.");
                return Task.CompletedTask;
            }

            string searchText = string.Join(" ", parameters);
            var searchResults = SearchManager.Search<Item2Entry>(searchText, context.Language, e => e.LocalizedTextIdName, true).Take(25);

            if (searchResults.Count() > 0)
            {
                context.Session.EnqueueMessageEncrypted(new Network.Message.Model.ServerChat
                {
                    Channel = ChatChannel.System,
                    Text = $"Item Lookup Results for '{searchText}' ({searchResults.Count()}):"
                });

                foreach (Item2Entry itemEntry in searchResults)
                {
                    string message = $"({itemEntry.Id}) [I]";
                    context.Session.EnqueueMessageEncrypted(new Network.Message.Model.ServerChat
                    {
                        Channel = ChatChannel.System,
                        Text = message,
                        Formats = new List<ChatFormat>
                    {
                        new ChatFormat
                        {
                            Type        = Game.Social.Static.ChatFormatType.ItemItemId,
                            StartIndex  = (ushort)(message.Length - 3),
                            StopIndex   = (ushort)message.Length,
                            FormatModel = new ChatFormatItemId
                            {
                                ItemId = itemEntry.Id
                            }
                        }
                    }
                    });
                }
            }
            else
            {
                context.Session.EnqueueMessageEncrypted(new Network.Message.Model.ServerChat
                {
                    Channel = ChatChannel.System,
                    Text = $"Item Lookup Results was 0 entries for '{searchText}'."
                });
            }
            return Task.CompletedTask;
        }
    }
}
