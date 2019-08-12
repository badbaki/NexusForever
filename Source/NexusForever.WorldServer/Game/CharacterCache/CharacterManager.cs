using NexusForever.WorldServer.Database.Character;
using NexusForever.WorldServer.Game.Entity;
using NLog;
using System;
using System.Collections.Generic;
using CharacterModel = NexusForever.WorldServer.Database.Character.Model.Character;

namespace NexusForever.WorldServer.Game.CharacterCache
{
    public static class CharacterManager
    {
        private static ILogger log { get; } = LogManager.GetCurrentClassLogger();

        private static readonly Dictionary<ulong, ICharacter> characters = new Dictionary<ulong, ICharacter>();
        private static readonly Dictionary<string, ulong> characterNameToId = new Dictionary<string, ulong>();

        /// <summary>
        /// Called to Initialise the <see cref="CharacterManager"/> at server start
        /// </summary>
        public static void Initialise()
        {
            BuildCharacterInfoFromDB();
        }

        /// <summary>
        /// Asynchronously adds <see cref="CharacterModel"/> data from the database to the cache
        /// </summary>
        private static async void BuildCharacterInfoFromDB()
        {
            List<CharacterModel> allCharactersInDb = await CharacterDatabase.GetAllCharactersAsync();
            foreach (CharacterModel character in allCharactersInDb)
            {
                if (character.DeleteTime != null)
                    continue;

                AddPlayer(character.Id, new CharacterInfo(character));
            }


            log.Info($"Stored {characters.Count} characters in Character Cache");
        }

        /// <summary>
        /// Add <see cref="ICharacter"/> to the cache with associated ID
        /// </summary>
        private static void AddPlayer(ulong characterId, ICharacter character)
        {
            characters.TryAdd(characterId, character);
            characterNameToId.Add(character.Name.ToLower(), characterId);
        }

        /// <summary>
        /// Remove <see cref="ICharacter"/> from the cache with a given ID. Should only be called when character is being deleted.
        /// </summary>
        public static void RemovePlayer(ulong characterId)
        {
            if (characters.TryGetValue(characterId, out ICharacter character))
                characters.Remove(characterId);

            if (characterNameToId.TryGetValue(character.Name.ToLower(), out ulong characterNameLookupId))
                characterNameToId.Remove(character.Name.ToLower());
        }

        /// <summary>
        /// Used to register a <see cref="Player"/> in the cache in place of an existing one or as an addition. Used to provide real time data to the client of a player. Should be used on player login.
        /// </summary>
        public static void RegisterPlayer(Player player)
        {
            if (characters.ContainsKey(player.CharacterId))
                characters[player.CharacterId] = player;
            else
                AddPlayer(player.CharacterId, player);
        }

        /// <summary>
        /// Used to deregister a <see cref="Player"/> from the cache and build a snapshot of the data. Should be used on player logout to keep the server's character data consistent.
        /// </summary>
        public static void DeregisterPlayer(Player player)
        {
            if (!characters.ContainsKey(player.CharacterId))
                throw new Exception($"{player.CharacterId} should exist in characters dictionary.");

            characters[player.CharacterId] = new CharacterInfo(player);
        }

        /// <summary>
        /// Returns a <see cref="Boolean"/> whether there is an <see cref="ICharacter"/> that exists with the name passed in.
        /// </summary>
        public static bool IsCharacter(string name)
        {
            characterNameToId.TryGetValue(name.ToLower(), out ulong value);
            return value > 0;
        }

        /// <summary>
        /// Returns the character ID of a player with the name passed in.
        /// </summary>
        public static ulong GetCharacterIdByName(string name)
        {
            characterNameToId.TryGetValue(name.ToLower(), out ulong characterId);
            return characterId;
        }

        /// <summary>
        /// Returns an <see cref="ICharacter"/> instance that matches the passed character ID, should one exist.
        /// </summary>
        public static ICharacter GetCharacterInfo(ulong characterId)
        {
            characters.TryGetValue(characterId, out ICharacter characterInfo);
            return characterInfo;
        }

        /// <summary>
        /// Returns an <see cref="ICharacter"/> instance that matches the passed character name, should one exist.
        /// </summary>
        public static ICharacter GetCharacterInfo(string name)
        {
            characterNameToId.TryGetValue(name.ToLower(), out ulong characterId);
            if (characterId > 0)
            {
                characters.TryGetValue(characterId, out ICharacter characterInfo);
                return characterInfo;
            }

            return null;
        }
    }
}