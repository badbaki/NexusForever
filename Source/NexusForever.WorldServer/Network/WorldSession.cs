using System;
using System.Collections.Generic;
using System.Net.Sockets;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character.Model;
using NexusForever.Shared.Cryptography;
using NexusForever.Shared.Network;
using NexusForever.Shared.Network.Message;
using NexusForever.Shared.Network.Message.Model;
using NexusForever.Shared.Network.Packet;
using NexusForever.WorldServer.Game.RBAC;
using NexusForever.WorldServer.Game.Account;
using NexusForever.WorldServer.Game.Entity;
using NexusForever.WorldServer.Game.RBAC.Static;
using NexusForever.WorldServer.Game.Static;
using NexusForever.WorldServer.Network.Message.Model;
using NexusForever.WorldServer.Game;

namespace NexusForever.WorldServer.Network
{
    public class WorldSession : GameSession
    {
        public AccountModel Account { get; private set; }
        public List<CharacterModel> Characters { get; } = new();

        public Player Player { get; set; }

        public AccountRBACManager AccountRbacManager { get; private set; }
        public GenericUnlockManager GenericUnlockManager { get; private set; }
        public AccountCurrencyManager AccountCurrencyManager { get; private set; }
        public EntitlementManager EntitlementManager { get; private set; }
        public AccountInventory AccountInventory { get; set; }

        public AccountTier AccountTier => AccountRbacManager.HasPermission(Permission.Signature) ? AccountTier.Signature : AccountTier.Basic;

        /// <summary>
        /// Determines if the <see cref="WorldSession"/> is queued to enter the realm.
        /// </summary>
        /// <remarks>
        /// This occurs when the world has reached the maximum number of allowed players.
        /// </remarks>
        public bool? IsQueued { get; set; }

        public override void OnAccept(Socket newSocket)
        {
            base.OnAccept(newSocket);

            EnqueueMessageEncrypted(new ServerHello
            {
                AuthVersion    = 16042,
                RealmId        = WorldServer.RealmId,
                RealmGroupId   = 21,
                AuthMessage    = 0x97998A0,
                ConnectionType = 11
            });
        }

        protected override IWritable BuildEncryptedMessage(byte[] data)
        {
            return new ServerRealmEncrypted
            {
                Data = data
            };
        }

        protected override void OnDisconnect()
        {
            base.OnDisconnect();
            Player?.CleanUp();

            // We check that Account isn't null because AuthServer pings World to check if online
            if (Account != null)
                LoginQueueManager.Instance.OnDisconnect(this);
        }

        /// <summary>
        /// Initialise <see cref="WorldSession"/> from an existing <see cref="AccountModel"/> database model.
        /// </summary>
        public void Initialise(AccountModel account)
        {
            if (Account != null)
                throw new InvalidOperationException();

            Account = account;
            NetworkManager<WorldSession>.Instance.UpdateSessionId(this, account.Id.ToString());

            // managers
            AccountRbacManager     = new AccountRBACManager(this, account);
            GenericUnlockManager   = new GenericUnlockManager(this, account);
            AccountCurrencyManager = new AccountCurrencyManager(this, account);
            EntitlementManager     = new EntitlementManager(this, account);
            AccountInventory = new AccountInventory(this, account);
        }

        public void SetEncryptionKey(byte[] sessionKey)
        {
            ulong key = PacketCrypt.GetKeyFromTicket(sessionKey);
            encryption = new PacketCrypt(key);
        }

        [MessageHandler(GameMessageOpcode.ClientPackedWorld)]
        public void HandlePackedWorld(ClientPackedWorld packedWorld)
        {
            var packet = new ClientGamePacket(packedWorld.Data);
            HandlePacket(packet);
        }
    }
}
