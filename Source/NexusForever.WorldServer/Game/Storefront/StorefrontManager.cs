using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusForever.Shared.Configuration;
using NexusForever.WorldServer.Database.World;
using NexusForever.WorldServer.Database.World.Model;
using NexusForever.WorldServer.Game.Storefront.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Model;
using NLog;

namespace NexusForever.WorldServer.Game.Storefront
{
    public static class StorefrontManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private static ImmutableList<Category> StoreCategoryList { get; set; }
        private static List<ServerStoreCategories.StoreCategory> ServerStoreCategoryList { get; set; } = new List<ServerStoreCategories.StoreCategory>();

        private static ImmutableList<OfferGroup> StoreOfferList { get; set; }
        private static List<ServerStoreOffers.OfferGroup> ServerStoreOfferList { get; set; } = new List<ServerStoreOffers.OfferGroup>();

        // Move to configuration and set value
        public static float ForcedProtobucksPrice { get; private set; } = 0f;
        public static float ForcedOmnibitsPrice { get; private set; } = 0f;
        public static bool CurrencyProtobucksEnabled { get; private set; } = true;
        public static bool CurrencyOmnibitsEnabled { get; private set; } = true;

        public static void Initialise()
        {
            LoadConfig();

            InitialiseStoreCategories();
            InitialiseStoreOfferGroups();

            BuildNetworkPackets();

            log.Info($"Initialised {StoreCategoryList.Count} categories with {StoreOfferList.Count} offers");
        }

        private static void LoadConfig()
        {
            ForcedProtobucksPrice = ConfigurationManager<WorldServerConfiguration>.Instance.Config.Store.ForcedProtobucksPrice;
            ForcedOmnibitsPrice = ConfigurationManager<WorldServerConfiguration>.Instance.Config.Store.ForcedOmnibitsPrice;
            CurrencyProtobucksEnabled = ConfigurationManager<WorldServerConfiguration>.Instance.Config.Store.CurrencyProtobucksEnabled;
            CurrencyOmnibitsEnabled = ConfigurationManager<WorldServerConfiguration>.Instance.Config.Store.CurrencyOmnibitsEnabled;
        }

        private static void BuildNetworkPackets()
        {
            foreach (Category category in StoreCategoryList)
                ServerStoreCategoryList.Add(category.BuildNetworkPacket());

            foreach (OfferGroup offerGroup in StoreOfferList)
                ServerStoreOfferList.Add(offerGroup.BuildNetworkPacket());
        }

        private static void InitialiseStoreCategories()
        {
            IEnumerable<StoreCategory> StoreCategories = WorldDatabase.GetStoreCategories()
                .OrderBy(i => i.Id)
                .Where(x => x.Id != 26 && Convert.ToBoolean(x.Visible) == true); // Remove parent category placeholder

            List<Category> categoryList = new List<Category>();
            foreach (StoreCategory category in StoreCategories)
                categoryList.Add(new Category(category));

            StoreCategoryList = categoryList.ToImmutableList();
        }

        private static void InitialiseStoreOfferGroups()
        {
            IEnumerable<StoreOfferGroup> StoreOfferGroups = WorldDatabase.GetStoreOfferGroups()
                .OrderBy(i => i.Id)
                .Where(x => Convert.ToBoolean(x.Visible) == true);

            List<OfferGroup> offerGroupList = new List<OfferGroup>();
            foreach (StoreOfferGroup offerGroup in StoreOfferGroups)
                offerGroupList.Add(new OfferGroup(offerGroup));

            StoreOfferList = offerGroupList.ToImmutableList();
        }

        public static void HandleCatalogRequest(WorldSession session)
        {
            SendStoreCategories(session);
            SendStoreOffers(session);
            SendStoreFinalise(session);
        }

        private static void SendStoreCategories(WorldSession session)
        {
            ServerStoreCategories serverCategories = new ServerStoreCategories
            {
                StoreCategories = ServerStoreCategoryList.ToList(),
                RealCurrency = RealCurrency.Usd
            };

            session.EnqueueMessageEncrypted(serverCategories);
        }

        private static void SendStoreOffers(WorldSession session)
        {
            List<ServerStoreOffers.OfferGroup> offersToSend = new List<ServerStoreOffers.OfferGroup>();
            uint count = 0;

            foreach (ServerStoreOffers.OfferGroup offerGroup in ServerStoreOfferList)
            {
                count++;
                offersToSend.Add(offerGroup);
                if (count == 20 || ServerStoreOfferList.IndexOf(offerGroup) == ServerStoreOfferList.Count - 1)
                {
                    session.EnqueueMessageEncrypted(new ServerStoreOffers
                    {
                        OfferGroups = offersToSend
                    });
                    offersToSend.RemoveRange(0, (int)count);
                    count = 0;
                }
            }
        }

        private static void SendStoreFinalise(WorldSession session)
        {
            session.EnqueueMessageEncrypted(new ServerStoreFinalise());
        }
    }
}