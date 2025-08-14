using Discord;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Archipelago.PacketClasses
{
    // Literally 99% of this was copilot based on the JSON structure of a server response. Hell yeah
    public class RoomInfo
    {
        public bool Password { get; set; }
        public List<string> Games { get; set; }
        public List<string> Tags { get; set; }
        public VersionInfo Version { get; set; }
        public int HintCost { get; set; }
        public Dictionary<string, string> datapackage_checksums { get; set; }
        public int LocationCheckPoints { get; set; }
        public string SeedName { get; set; }
        public double Time { get; set; }

        public RoomInfo(Dictionary<string, object> jsonPacket)
        {
            JsonElement temp = (JsonElement)jsonPacket["password"];
            Password = temp.GetBoolean();
            temp = (JsonElement)jsonPacket["games"];
            Games = temp.EnumerateArray().Select(g => g.GetString()).ToList();
            temp = (JsonElement)jsonPacket["tags"];
            Tags = temp.EnumerateArray().Select(t => t.GetString()).ToList();
            temp = (JsonElement)jsonPacket["version"];
            Version = new VersionInfo(temp);
            temp = (JsonElement)jsonPacket["hint_cost"];
            HintCost = temp.GetInt32();
            datapackage_checksums = new();
            temp = (JsonElement)jsonPacket["datapackage_checksums"];
            foreach (var checksum in temp.EnumerateObject())
            {
                JsonElement temp2 = (JsonElement)checksum.Value;
                datapackage_checksums.Add(checksum.Name, temp2.GetString());
            }
            temp = (JsonElement)jsonPacket["location_check_points"];
            LocationCheckPoints = temp.GetInt32();
            temp = (JsonElement)jsonPacket["seed_name"];
            SeedName = temp.GetString();
            temp = (JsonElement)jsonPacket["time"];
            Time = temp.GetDouble();
        }
    }

    public class VersionInfo
    {
        public int Major { get; }
        public int Minor { get; }
        public int Build { get; }
        public string Class { get; }

        public VersionInfo(JsonElement version)
        {
            Major = version.GetProperty("major").GetInt32();
            Minor = version.GetProperty("minor").GetInt32();
            Build = version.GetProperty("build").GetInt32();
            Class = version.GetProperty("class").GetString();
        }

        // Lower case keys, because python....
        public Dictionary<String, object> SpecialDictionary()
        {
            return new Dictionary<string, object>
            {
                { "major", Major },
                { "minor", Minor },
                { "build", Build },
                { "class", Class }
            };
        }
    }
    public class GameContext
    {
        public int Team { get; set; }
        public int Slot { get; set; }
        public List<PlayerInfo> Players { get; set; }
        public List<int> MissingLocations { get; set; }
        public List<int> CheckedLocations { get; set; }
        public Dictionary<int, SlotInfo> SlotInfo { get; set; }
        public int HintPoints { get; set; }

        public GameContext(Dictionary<string, object> archipelagoConnectedPacket)
        {
            JsonElement temp = (JsonElement)archipelagoConnectedPacket["team"];
            Team = temp.GetInt32();
            temp = (JsonElement)archipelagoConnectedPacket["slot"];
            Slot = temp.GetInt32();
            temp = (JsonElement)archipelagoConnectedPacket["players"];
            Players = temp.EnumerateArray().Select(p => new PlayerInfo(p)).ToList();
            temp = (JsonElement)archipelagoConnectedPacket["missing_locations"];
            MissingLocations = temp.EnumerateArray().Select(m => m.GetInt32()).ToList();
            temp = (JsonElement)archipelagoConnectedPacket["checked_locations"];
            CheckedLocations = temp.EnumerateArray().Select(c => c.GetInt32()).ToList();
            temp = (JsonElement)archipelagoConnectedPacket["slot_info"];
            SlotInfo = new Dictionary<int, SlotInfo>();
            foreach (var slot in temp.EnumerateObject())
            {
                SlotInfo.Add(int.Parse(slot.Name), new SlotInfo(slot.Value));
            }
            temp = (JsonElement)archipelagoConnectedPacket["hint_points"];
            HintPoints = temp.GetInt32();
        }

        public string GetPlayerName(int id)
        {
            foreach (PlayerInfo player in Players)
            {
                if (player.Slot == id)
                {
                    return player.Name;
                }
            }
            return "Unknown";
        }

        public string GetPlayerGame(int id)
        {
            if (SlotInfo.ContainsKey(id))
            {
                return SlotInfo[id].Game;
            }
            return "Unknown";
        }

        public string GetItemName(int playerID, int itemID)
        {
            string game = GetPlayerGame(playerID);
            return GameDataManager.GetItemName(game, itemID);
        }
    }

    public class PlayerInfo
    {
        public int Team { get; set; }
        public int Slot { get; set; }
        public string Alias { get; set; }
        public string Name { get; set; }
        public string Class { get; set; }

        public PlayerInfo(JsonElement player)
        {
            Team = player.GetProperty("team").GetInt32();
            Slot = player.GetProperty("slot").GetInt32();
            Alias = player.GetProperty("alias").GetString();
            Name = player.GetProperty("name").GetString();
            Class = player.GetProperty("class").GetString();
        }
    }

    public class SlotInfo
    {
        public string Name { get; set; }
        public string Game { get; set; }
        public int Type { get; set; }
        public List<int> GroupMembers { get; set; }
        public string Class { get; set; }

        public SlotInfo(JsonElement player)
        {
            Name = player.GetProperty("name").GetString();
            Game = player.GetProperty("game").GetString();
            Type = player.GetProperty("type").GetInt32();
            GroupMembers = player.GetProperty("group_members").EnumerateArray().Select(g => g.GetInt32()).ToList();
            Class = player.GetProperty("class").GetString();
        }
    }
    public class ReceivedItemsCommand
    {
        public string Cmd { get; set; }
        public int Index { get; set; }
        public List<ItemInfo> Items { get; set; }

        public ReceivedItemsCommand(Dictionary<string, object> jsonPacket)
        {
            Cmd = ((JsonElement)jsonPacket["cmd"]).GetString();
            Index = ((JsonElement)jsonPacket["index"]).GetInt32();
            Items = new List<ItemInfo>();
            foreach (var item in ((JsonElement)jsonPacket["items"]).EnumerateArray())
            {
                Items.Add(new ItemInfo(item));
            }
        }
    }

    public class ItemInfo
    {
        public int Item { get; set; }
        public int Location { get; set; }
        public int Player { get; set; }
        public int Flags { get; set; }
        public string Class { get; set; }

        public ItemInfo(JsonElement item)
        {
            Item = item.GetProperty("item").GetInt32();
            Location = item.GetProperty("location").GetInt32();
            Player = item.GetProperty("player").GetInt32();
            Flags = item.GetProperty("flags").GetInt32();
            Class = item.GetProperty("class").GetString();
        }

        public ItemInfo(Dictionary<string, object> item)
        {
            Item = (int)item["item"];
            Location = (int)item["location"];
            Player = (int)item["player"];
            Flags = (int)item["flags"];
            Class = (string)item["class"];
        }
    }

    public class GameObjectIDHash
    {
        public string Checksum { get; set; }
        public Dictionary<string, int> ItemNameToID;
        public Dictionary<int, string> ItemIDToName;
        public Dictionary<string, int> LocationNameToID;
        public Dictionary<int, string> LocationIDToName;

        public GameObjectIDHash(string json)
        {
            Dictionary<string, object> temp = null;
            try
            {
                temp = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            } catch
            {
                DiscordBot.Log("Failed to parse game cache data from JSON!", "GameObjectIDHash", Discord.LogSeverity.Error);
            }

            if (temp != null)
            {
                Init(temp);
            }
        }

        public GameObjectIDHash(Dictionary<string, object> data)
        {
            Init(data);
        }

        protected void Init(Dictionary<string, object> data)
        {
            try
            {
                Checksum = data["checksum"].ToString();

                // {'item_name_to_id': {'itemname': 1}, 'location_name_to_id': {blah...}}
                ItemNameToID = new Dictionary<string, int>();
                ItemIDToName = new Dictionary<int, string>();
                JsonElement temp = (JsonElement)data["item_name_to_id"];
                foreach (var obj in temp.EnumerateObject())
                {
                    if (!ItemNameToID.ContainsKey(obj.Name))
                    {
                        ItemNameToID.Add(obj.Name, obj.Value.GetInt32());
                    }
                    else
                    {
                        DiscordBot.Log($"Duplicate item name found in game cache data! {obj.Name}", "GameObjectIDHash", Discord.LogSeverity.Warning);
                    }

                    if (!ItemIDToName.ContainsKey(obj.Value.GetInt32()))
                    {
                        ItemIDToName.Add(obj.Value.GetInt32(), obj.Name);
                    }
                    else
                    {
                        DiscordBot.Log($"Duplicate item ID found in game cache data! {obj.Value.GetInt32()}", "GameObjectIDHash", Discord.LogSeverity.Warning);
                    }
                }

                LocationNameToID = new Dictionary<string, int>();
                LocationIDToName = new Dictionary<int, string>();
                temp = (JsonElement)data["location_name_to_id"];
                foreach (var obj in temp.EnumerateObject())
                {
                    if (!LocationNameToID.ContainsKey(obj.Name))
                    {
                        LocationNameToID.Add(obj.Name, obj.Value.GetInt32());
                    }
                    else
                    {
                        DiscordBot.Log($"Duplicate location name found in game cache data! {obj.Name}", "GameObjectIDHash", Discord.LogSeverity.Warning);
                    }

                    if (!LocationIDToName.ContainsKey(obj.Value.GetInt32()))
                    {
                        LocationIDToName.Add(obj.Value.GetInt32(), obj.Name);
                    }
                    else
                    {
                        DiscordBot.Log($"Duplicate location ID found in game cache data! {obj.Value.GetInt32()}", "GameObjectIDHash", Discord.LogSeverity.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                DiscordBot.Log("Failed to parse game cache data from Dict<str,obj>!", "GameObjectIDHash", Discord.LogSeverity.Error);
                DiscordBot.Log(ex.Message, "GameObjectIDHash", Discord.LogSeverity.Error);
            }
        }
    }

    public class JSONMessagePart : Dictionary<string, JsonElement> { }


    public class PrintJSONPacket
    {
        // Original packet that would have been sent by Archipelago Server
        public Dictionary<string,object> cmdPacket { get; set; }
        // Message chunks (dictionary containing type of message-part, and associated data)
        public List<JSONMessagePart> messageData { get; set; }
        public string type { get; set; }
        public int team { get; set; }
        public int slot { get; set; }
        public ArrayList tags { get; set; }

    
        public PrintJSONPacket(Dictionary<string,object> cmdPacket)
        {
            // TODO, CONTINUE HERE
            cmdPacket = cmdPacket;
            type = cmdPacket.ContainsKey("type") ? ((JsonElement)cmdPacket["type"]).GetString() : "unknown";
            team = cmdPacket.ContainsKey("team") ? ((JsonElement)cmdPacket["team"]).GetInt32() : -1;
            slot = cmdPacket.ContainsKey("slot") ? ((JsonElement)cmdPacket["slot"]).GetInt32() : -1;

            // Convert the List<Dictionary> stored in the JSON into a.... List<Dictionary>...
            tags = new();
            if (cmdPacket.ContainsKey("tags"))
            {
                foreach (var tag in ((JsonElement)cmdPacket["tags"]).EnumerateArray())
                {
                    tags.Add(tag.GetString());
                }
            }

            // Convert the List<Dictionary<...>> stored in the JSON into a.... List<Dictionary<...>>...
            messageData = new();
            foreach (var packet in ((JsonElement)cmdPacket["data"]).EnumerateArray())
            {
                foreach (var dict in packet.EnumerateObject())
                {
                    messageData.Add(new JSONMessagePart
                    {
                        { dict.Name, dict.Value }
                    });
                }
            }
        }
    }
}
