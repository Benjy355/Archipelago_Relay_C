using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using Archipelago.PacketClasses;

namespace Archipelago
{
    class Relay
    {
        public GameData GameData { get; protected set; } // Data gathered from the Archipelago Website
        public int SlotID { get; protected set; } // Which slot will this relay connect to
        public RoomInfo RoomInfo { get; protected set; } // Provided by the "RoomInfo" cmd
        public ConnectedCommand ConnectedGameInformation { get; protected set; } // Provided by the "Connected" cmd
        public ReceivedItemsCommand ReceivedItems { get; protected set; }

        protected List<String> PendingOutgoingPayloads; // List of strings (JSON) to be sent to the archipelago server
        protected ClientWebSocket webSocket;

        public Relay(GameData gameData, int slot = 0)
        {
            this.GameData = gameData;
            this.SlotID = slot;
            this.PendingOutgoingPayloads = new();
            this.webSocket = new();
        }

        protected async Task HandleJsonCmd(Dictionary<string, object> data)
        {
            if (!data.ContainsKey("cmd"))
            {
                throw new Exception("JSON command missing 'cmd' field");
            }

            try
            {
                Dictionary<String, object> responsePayload;
                switch (data["cmd"].ToString())
                {
                    case "RoomInfo":
                        RoomInfo = new PacketClasses.RoomInfo(data);
                        await DiscordBot.Log("Successfully ingested RoomInfo", "Relay", Discord.LogSeverity.Debug);
                        // Now that we have room info, connect as our intended player/slot.
                        responsePayload = new Dictionary<string, object> //TODO: Add password support
                        {
                            { "cmd", "Connect" },
                            { "password", "" },
                            { "name", GameData.slots[SlotID].playerName },
                            { "version", RoomInfo.Version.SpecialDictionary() },
                            { "tags", new List<String>(["TextOnly", "AP", "DeathLink"]) },
                            { "items_handling", 0b111 },
                            { "uuid", 696942025 },
                            { "game", GameData.slots[SlotID].playerGame },
                            { "slot_data", false }
                        };

                        await SchedulePayload(responsePayload);
                        break;

                    case "Connected":
                        ConnectedGameInformation = new ConnectedCommand(data);
                        await DiscordBot.Log("Successfully ingested Connected packet", "Relay", Discord.LogSeverity.Debug);

                        List<string> gamesInMultiworld = new();

                        // Get a list of games in the multiworld
                        foreach (SlotInfo slot in ConnectedGameInformation.SlotInfo.Values)
                        {
                            if (!gamesInMultiworld.Contains(slot.Game))
                            {
                                gamesInMultiworld.Add(slot.Game);
                            }
                        }
                        
                        // Check with our GameDataManager to see if we have the game information cached already, or if we need more



                        break;

                    case "ReceivedItems":
                        ReceivedItems = new ReceivedItemsCommand(data);
                        await DiscordBot.Log("Successfully ingested ReceivedItems packet", "Relay", Discord.LogSeverity.Debug);
                        break;

                    default:
                        throw new Exception($"Unknown JSON command: {data["cmd"].ToString()}");
                }
            } catch (Exception ex)
            {
                await DiscordBot.Log($"{ex.Message}", "Relay", Discord.LogSeverity.Error);
            }
        }

        protected async void HandleReceivedData(String receivedData)
        {
            await DiscordBot.Log($"Received data: {receivedData}", "Relay", Discord.LogSeverity.Debug);

            // Parse the JSON
            List<Dictionary<string, object>>? jsonPackets;
            try
            {
                jsonPackets = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(receivedData);
            }
            catch (Exception ex)
            {
                await DiscordBot.Log($"Failed to parse response JSON: {ex.Message}", "Relay", Discord.LogSeverity.Error);
                return;
            }

            foreach (Dictionary<string, object> packet in jsonPackets)
            {
                try
                {
                    await HandleJsonCmd(packet);
                } 
                catch (Exception ex)
                {
                    await DiscordBot.Log($"Failed to handle JSON command: {ex.Message}", "Relay", Discord.LogSeverity.Error);
                }
            }
        }


        protected async Task ReceiveDataLoop()
        {
            while (webSocket.State != WebSocketState.Closed)
            {
                StringBuilder receivedData = new StringBuilder();

                byte[] buffer = new byte[1024];
                WebSocketReceiveResult result;

                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    receivedData.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                HandleReceivedData(receivedData.ToString());

                await Task.Delay(5);
            }
            await DiscordBot.Log("ReceiveDataLoop exited", "Relay", Discord.LogSeverity.Debug);
            await Task.CompletedTask;
        }

        protected async Task SendDataLoop()
        {
            while (webSocket.State != WebSocketState.Closed)
            {
                if (PendingOutgoingPayloads.Count > 0)
                {
                    string payload = PendingOutgoingPayloads[0];
                    await DiscordBot.Log($"Sending payload: {payload}", "Relay", Discord.LogSeverity.Debug);
                    await webSocket.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, CancellationToken.None);
                    PendingOutgoingPayloads.RemoveAt(0);
                }
                else
                {
                    await Task.Delay(5);
                }
            }
            await DiscordBot.Log("SendDataLoop exited", "Relay", Discord.LogSeverity.Debug);
            await Task.CompletedTask;
        }

        protected async Task SchedulePayload(Dictionary<string, object> payload)
        {
            // TODO: Implement whatever the fuck is going on with _scan_for_TypedTuples in the python version here.
            // Archipelago really prefers List<Dictionary<String,Object>>
            List<Dictionary<String, object>> iterablePayload = new();
            iterablePayload.Add(payload);
            await SchedulePayload(JsonSerializer.Serialize(iterablePayload));
        }
        protected async Task SchedulePayload(String payload)
        {
            PendingOutgoingPayloads.Add(payload);
        }

        // Connect to the archipelago world.
        public async Task Connect()
        {
            await DiscordBot.Log($"Connecting to Archipelago... Game ID: {GameData.gameID}", "Relay", Discord.LogSeverity.Debug);
            await webSocket.ConnectAsync(new Uri(GameData.WebSocketsURI()), CancellationToken.None);

            await Task.WhenAll(SendDataLoop(), ReceiveDataLoop());
        }
    }
}
