using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Archipelago;
using HtmlAgilityPack;

namespace Archipelago
{
    static class SiteScraper
    {
        public static async Task<GameData> LoadSiteData(string URI)
        {
            try
            {
                GameData newData = new();
                HtmlDocument htmlDoc;

                HtmlWeb webParser = new HtmlWeb();
                webParser.Timeout = 2000;

                htmlDoc = await webParser.LoadFromWebAsync(URI);

                newData.gameID = htmlDoc.DocumentNode.SelectSingleNode("//head/title").InnerText.Substring(11);
                //This is disgusting.
                newData.port = htmlDoc.DocumentNode.SelectSingleNode("//span[@id='host-room-info']").InnerText.Split("archipelago.gg:")[1].Substring(0, 5);

                foreach (HtmlNode trNode in htmlDoc.DocumentNode.SelectNodes("//tbody/tr"))
                {
                    HtmlNodeCollection tdNodes = trNode.SelectNodes("td");
                    SlotData newSlot = new();
                    newSlot.slotID = uint.Parse(tdNodes[0].InnerText);
                    newSlot.playerName = tdNodes[1].SelectSingleNode("a").InnerText;
                    newSlot.playerGame = tdNodes[2].InnerText;

                    newData.slots.Add(newSlot);
                }
                return newData;
            }
            catch (Exception ex)
            {
                await DiscordBot.Log($"Failed to load site data: {ex.Message}", "SiteScraper", Discord.LogSeverity.Error);
                return null;
            }
        }
    }
}