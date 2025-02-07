using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archipelago
{
    // Utility classes for Archipelago stuff

    public class SlotData // Player slot information
    {
        public uint slotID { get; set; }
        public String playerName { get; set; }
        public String playerGame { get; set; }

        public SlotData(uint slotID = 0, string playerName = "", string playerGame = "")
        {
            this.slotID = slotID;
            this.playerName = playerName;
            this.playerGame = playerGame;
        }
    }

    public class GameData // Full world information
    {
        public String gameID { get; set; }
        public String port { get; set; }
        public List<SlotData> slots { get; set; }

        public GameData()
        {
            this.gameID = "";
            this.port = "";
            this.slots = new List<SlotData>();
        }

        public String WebSocketsURI()
        {
            return "wss://archipelago.gg:" + port;
        }
    }
}
