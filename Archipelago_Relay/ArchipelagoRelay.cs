using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Archipelago
{
    class Relay
    {
        public GameData GameData { get; protected set; } // Data gathered from the Archipelago Website
        public int SlotID { get; protected set; } // Which slot will this relay connect to

        protected List<String> PendingOutgoingPayloads; // List of strings (JSON) to be sent to the archipelago server
        protected ClientWebSocket webSocket;

        public Relay(GameData gameData, int slot = 0)
        {
            this.GameData = gameData;
            this.SlotID = slot;
            this.PendingOutgoingPayloads = new();
            this.webSocket = new();
            this.webSocket.Options.DangerousDeflateOptions = new WebSocketDeflateOptions
            {
                ClientMaxWindowBits = 15,
                ServerMaxWindowBits = 15,
                ClientContextTakeover = true,
                ServerContextTakeover = true
            };
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
                    

                    default:
                        throw new Exception($"Unknown JSON command: {data["cmd"].ToString()}");
                }
            }
            catch (Exception ex)
            {
                var stackTrace = new System.Diagnostics.StackTrace(ex, true);
                var frame = stackTrace.GetFrames()?.FirstOrDefault(f => f.GetFileLineNumber() > 0);
                var fileName = frame?.GetFileName();
                var lineNumber = frame?.GetFileLineNumber();

                await DiscordBot.Log($"{ex.Message} (File: {fileName}, Line {lineNumber})", "HandleJsonCmd", Discord.LogSeverity.Error);
            }
        }

        protected async void HandleReceivedData(String receivedData)
        {
            await DiscordBot.Log($"Received data: {receivedData}", "HandleReceivedData", Discord.LogSeverity.Debug);

            // Parse the JSON
            List<Dictionary<string, object>>? jsonPackets;
            try
            {
                jsonPackets = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(receivedData);
            }
            catch (Exception ex)
            {
                await DiscordBot.Log($"Failed to parse response JSON: {ex.Message}", "HandleReceivedData", Discord.LogSeverity.Error);
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
                    await DiscordBot.Log($"Failed to handle JSON command: {ex.Message}", "HandleReceivedData", Discord.LogSeverity.Error);
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
            // TODO: Implement whatever the fuck is going on with _scan_for_TypedTuples in the python version here; (WAIT, DO WE? That might not be a thing anymore)
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
            await DiscordBot.Log($"Connecting to Archipelago... Game ID: {GameData.gameID}", "Relay", Discord.LogSeverity.Verbose);
            await webSocket.ConnectAsync(new Uri(GameData.WebSocketsURI()), CancellationToken.None);

            await Task.WhenAll(SendDataLoop(), ReceiveDataLoop());
        }
    }
}
