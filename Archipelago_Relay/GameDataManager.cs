using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Archipelago
{
    // Class used to handle acquiring and storing game data from Archipelago. (Item and location number->Name)
    static class GameDataManager
    {
        const string GameDataFolder = "game_item_data";

        public static Dictionary<string, Dictionary<string, object>> GameDataCache = new();


        // Checks for game file either in cache, or failing that, for the file. If found, loads it up and checks to confirm it's up to date. Returns true if we have it, false if we don't.
        static bool CheckAndLoadGameCache(string game, string checksum)
        {
            // Escape game name
            game = game.Replace("\\", "_");
            game = game.Replace("/", "_");
            game = game.Replace(":", "_");

            if (GameDataCache.ContainsKey(game) && GameDataCache[game]["checksum"].ToString() == checksum) {
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
                Dictionary<string, object> data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (!data.ContainsKey("checksum"))
                {
                    return false;
                }

                string fileChecksum = data["checksum"].ToString();

                if (fileChecksum != checksum)
                {
                    return false;
                }

                // We got a good checksum, load the data into the cache
                if (GameDataCache.ContainsKey(game))
                {
                    GameDataCache[game] = data;
                }
                else
                {
                    GameDataCache.Add(game, data);
                }
            } 
            catch (Exception ex)
            {
                DiscordBot.Log($"Failed to read game data from {filePath}: {ex.Message}", "GameDataManager", Discord.LogSeverity.Error);
                return false;
            }

            return true;
        }

        static void UpdateGameData(string json, string gameName)
        {
            Dictionary<string, object> data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            //TODO: YOU WERE HERE

            //data["item_id_to_name"] = itemIdToName;
        }
    }
}
