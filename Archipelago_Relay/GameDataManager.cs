using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Archipelago.PacketClasses;

namespace Archipelago
{
    // Class used to handle acquiring and storing game data from Archipelago. (Item and location number->Name)
    static class GameDataManager
    {
        const string GameDataFolder = "game_item_data";

        public static Dictionary<string, GameObjectIDHash> GameDataCache = new();


        // Checks for game file either in cache, or failing that, for the file. If found, loads it up and checks to confirm it's up to date. Returns true if we have it, false if we don't.
        public static bool CheckAndLoadGameCache(string game, string checksum)
        {
            // Escape game name
            game = game.Replace("\\", "_");
            game = game.Replace("/", "_");
            game = game.Replace(":", "_");

            if (GameDataCache.ContainsKey(game) && GameDataCache[game].Checksum == checksum) {
                return true;
            }

            string filePath = Path.Combine(GameDataFolder, game + ".json");

            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                GameObjectIDHash tempHash = new(json);

                if (tempHash.Checksum != checksum)
                {
                    return false;
                }

                GameDataCache[game] = tempHash;
                DiscordBot.Log($"Successfully loaded cache for {game}", "GameDataManager", Discord.LogSeverity.Debug);
            } 
            catch (Exception ex)
            {
                DiscordBot.Log($"Failed to read game data from {filePath}: {ex.Message}", "GameDataManager", Discord.LogSeverity.Error);
                return false;
            }

            return true;
        }

        public static void UpdateGameData(string json, string gameName)
        {
            GameObjectIDHash tempHash = new(json);
            GameDataCache[gameName] = tempHash;

            // Now we save it to a JSON file!
            string filePath = Path.Combine(GameDataFolder, gameName + ".json");
            try
            {
                if (!Directory.Exists(GameDataFolder))
                {
                    Directory.CreateDirectory(GameDataFolder);
                }
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                DiscordBot.Log($"Failed to write game data to {filePath}: {ex.Message}", "GameDataManager", Discord.LogSeverity.Error);
            }
        }
    }
}
