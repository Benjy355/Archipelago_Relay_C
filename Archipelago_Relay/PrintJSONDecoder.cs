using Archipelago.PacketClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archipelago
{
    /// <summary>
    /// Class to handle taking PrintJSON command packets and converting them into usable Discord messages.
    /// </summary>
    static class PrintJSONDecoder
    {
        public static String ConvertJSONMessage(PrintJSONPacket packet, GameContext gameContext)
        {
            String finalString = "";
            // Go through each packet data type and put together the string

            foreach (JSONMessagePart part in packet.messageData)
            {
                foreach (var dataPacket in part)
                {
                    switch (dataPacket.Key)
                    {
                        case "text":
                            finalString += dataPacket.Value.GetString();
                            break;

                        case "player_id":
                            finalString += gameContext.GetPlayerName(dataPacket.Value.GetInt32());
                            break;

                        case "item_id":
                            
                            //finalString += gameContext.GetItemName(;
                            // TODO: Get packet for a received item PrintJSON, can't remember what it looks like.
                            break;

                        default:
                            DiscordBot.Log($"Unknown MessageType: {dataPacket.Key}", "JSONDecoder", Discord.LogSeverity.Warning);
                            break;
                    }
                }
            }
            return finalString;
        }
    }
}
