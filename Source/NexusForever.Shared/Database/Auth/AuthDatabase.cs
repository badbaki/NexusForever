using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Shared.Configuration;
using NexusForever.Shared.Cryptography;
using NexusForever.Shared.Database.Auth.Model;

namespace NexusForever.Shared.Database.Auth
{
    public static class AuthDatabase
    {
        public static async Task Save(Action<AuthContext> action)
        {
            using (var context = new AuthContext())
            {
                action.Invoke(context);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Selects an <see cref="Account"/> asynchronously that matches the supplied email.
        /// </summary>
        public static async Task<Account> GetAccountAsync(string email)
        {
            email = email.ToLower();
            using (var context = new AuthContext())
                return await context.Account.SingleOrDefaultAsync(a => a.Email == email);
        }

        /// <summary>
        /// Selects an <see cref="Account"/> asynchronously that matches the supplied email and game token.
        /// </summary>
        public static async Task<Account> GetAccountAsync(string email, Guid guid)
        {
            email = email.ToLower();
            string gameToken = guid.ToByteArray().ToHexString();
            using (var context = new AuthContext())
                return await context.Account.SingleOrDefaultAsync(a => a.Email == email && a.GameToken == gameToken);
        }

        /// <summary>
        /// Selects an <see cref="Account"/> asynchronously that matches the supplied email and session key.
        /// </summary>
        public static async Task<Account> GetAccountAsync(string email, byte[] sessionKeyBytes)
        {
            email = email.ToLower();
            string sessionKey = BitConverter.ToString(sessionKeyBytes).Replace("-", "");
            using (var context = new AuthContext())
                return await context.Account
                    .Include(a => a.AccountCostumeUnlock)
                    .Include(a => a.AccountCurrency)
                    .Include(a => a.AccountGenericUnlock)
                    .Include(a => a.AccountKeybinding)
                    .Include(a => a.AccountEntitlement)
                    .Include(a => a.AccountPermission)
                    .Include(a => a.AccountRole)
                    .SingleOrDefaultAsync(a => a.Email == email && a.SessionKey == sessionKey);
        }

        /// <summary>
        /// Create a new account with the supplied email and password, the password will have a verifier generated that is inserted into the database.
        /// </summary>
        public static async Task<Account> CreateAccount(string email, string password, ulong defaultRole, ulong[] extraRoles = null)
        {
            email = email.ToLower();
            using (var context = new AuthContext())
            {
                // Ensure account doesn't already exist with the same email
                if (context.Account.FirstOrDefault(a => a.Email == email) != null)
                    return null;

                // Ensure only allowed symbols are used in the email - https://en.wikipedia.org/wiki/Email_address#Local-part
                if (Regex.IsMatch(email, @"[^A-Za-z0-9@.!#$%&'*+-/=?^_`{|}~]"))
                    return null;

                byte[] s = RandomProvider.GetBytes(16u);
                byte[] v = Srp6Provider.GenerateVerifier(s, email, password);

                Account newAccount = new Account
                {
                    Email = email,
                    S     = s.ToHexString(),
                    V     = v.ToHexString()
                };

                // Add default role
                newAccount.AccountRole.Add(new AccountRole
                {
                    RoleId = defaultRole
                });

                if (extraRoles != null)
                {
                    foreach (ulong role in extraRoles)
                        newAccount.AccountRole.Add(new AccountRole
                        {
                            RoleId = role
                        });
                }

                context.Account.Add(newAccount);

                context.SaveChanges();

                return await context.Account.FirstOrDefaultAsync(a => a.Email == email);
            }
        }

        /// <summary>
        /// Delete an existing account with the supplied email.
        /// </summary>
        public static bool DeleteAccount(string email)
        {
            email = email.ToLower();
            // Thanks Rawaho!
            using (var context = new AuthContext())
            {
                Account account = context.Account.SingleOrDefault(a => a.Email == email);
                if (account == null)
                    return false;

                context.Account.Remove(account);
                return context.SaveChanges() > 0;
            }
        }

        /// <summary>
        /// Update <see cref="Account"/> with supplied game token asynchronously.
        /// </summary>
        public static async Task UpdateAccountGameToken(Account account, Guid guid)
        {
            account.GameToken = guid.ToByteArray().ToHexString();
            using (var context = new AuthContext())
            {
                EntityEntry<Account> entity = context.Attach(account);
                entity.Property(p => p.GameToken).IsModified = true;
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Update <see cref="Account"/> with supplied session key asynchronously.
        /// </summary>
        public static async Task UpdateAccountSessionKey(Account account, byte[] sessionKeyBytes)
        {
            account.SessionKey = BitConverter.ToString(sessionKeyBytes).Replace("-", "");
            using (var context = new AuthContext())
            {
                EntityEntry<Account> entity = context.Attach(account);
                entity.Property(p => p.SessionKey).IsModified = true;
                await context.SaveChangesAsync();
            }
        }

        public static ImmutableList<Server> GetServers()
        {
            using (var context = new AuthContext())
                return context.Server
                    .AsNoTracking()
                    .ToImmutableList();
        }

        public static ImmutableList<ServerMessage> GetServerMessages()
        {
            using (var context = new AuthContext())
                return context.ServerMessage
                    .AsNoTracking()
                    .ToImmutableList();
        }

        public static ImmutableList<Role> GetRoles()
        {
            using (var context = new AuthContext())
            {
                return context.Role
                    .Include(a => a.RolePermission)
                    .AsNoTracking()
                    .ToImmutableList();
            }
        }
    }
}
