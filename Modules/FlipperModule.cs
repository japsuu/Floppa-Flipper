using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FloppaFlipper.Modules
{
    public class FlipperModule : ModuleBase<SocketCommandContext>
    {
        private const string PricesApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/latest";
        private const string ItemInfoApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/mapping";
        
        private static readonly Dictionary<int, ItemInfo> InfoDict = new();
        private static List<ItemInfo> infoList = new();

        public static readonly Timer Timer = new();
        
        [Command("embed")]
        public async Task SendRichEmbedAsync()
        {
            var embed = new EmbedBuilder();

            embed.AddField("Generic item info",
                    "Link to item in the [wiki](https://wiki.com)")
                .WithFooter(footer => footer.Text = "I am a footer.")
                .WithColor(Color.Blue)
                .WithTitle("I overwrote \"Hello world!\"")
                .WithDescription("I am a description.")
                .WithUrl("https://example.com")
                .WithCurrentTimestamp()
                .WithThumbnailUrl("https://i1.sndcdn.com/artworks-eXRx47ZqsXmCG10I-CUQ2fA-t500x500.jpg");

            //Your embed needs to be built before it is able to be sent
            await ReplyAsync(embed: embed.Build());
        }
        
        [Command("update")]
        [Summary("Forcefully updates the prices of all items.")]
        public Task UpdatePrices()
        {
            FetchItemPrices();
            
            var tUp = new Emoji("👍");
            return Context.Message.AddReactionAsync(tUp);
        }
        
        [Command("show")]
        [Summary("Show the items with changed margins.")]
        public Task ShowChangedPrices(int amount = 0, double minChangePercentage = 10)
        {
            foreach (ItemInfo info in infoList.Where(i => (i.CurrentBuy != i.PreviousBuy) && (Math.Abs(i.GetChangePercentage()) > minChangePercentage)).Take(amount))
            {
                var embed = new EmbedBuilder();

                embed.AddField($"Changed from {info.PreviousBuy} to {info.CurrentBuy}",
                        "Link to item in the [wiki](https://wiki.com)")
                    .WithFooter(footer => footer.Text = "Flop fo no hoe")
                    .WithColor(Color.Blue)
                    .WithTitle(info.Name)
                    .WithDescription($"price has changed {info.GetChangePercentage():F2}%")
                    .WithCurrentTimestamp()
                    .WithThumbnailUrl("https://secure.runescape.com/m=itemdb_oldschool/1633359101944_obj_big.gif?id=8");
                
                
                ReplyAsync(embed: embed.Build());
            }

            Emoji tUp = new Emoji("👍");

            if (amount < 1)
            {
                ReplyAsync("Invalid count", false, null, null, null, Context.Message.Reference);
                
                tUp = new Emoji("❌");
            }

            
            return Context.Message.AddReactionAsync(tUp);
        }
        
        public static void FetchItemData()
        {
            Console.WriteLine("Querying item data...");
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ItemInfoApiEndpoint);
            
            request.UserAgent = "FloppaFlipper - Japsu#8887";
            request.Method = "GET";
            
            HttpWebResponse infoResponse = (HttpWebResponse)request.GetResponse();
            
            Console.WriteLine("Connection: " + infoResponse.StatusCode);

            string infoJsonString;
            using (Stream stream = infoResponse.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                infoJsonString = reader.ReadToEnd();
            }
            
            infoList = JsonConvert.DeserializeObject<List<ItemInfo>>(infoJsonString);
            
            if (infoList == null) return;
            
            foreach (ItemInfo info in infoList)
            {
                InfoDict.Add(info.Id, info);
            }
            
            Console.WriteLine("Done!\n");
        }
        
        public static void TimerTick(object sender, EventArgs e)
        {
            FetchItemPrices();
        }
        
        private static void FetchItemPrices()
        {
            Console.WriteLine("Querying prices...");
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(PricesApiEndpoint);

            request.UserAgent = "FloppaFlipper - Japsu#8887";
            request.Method = "GET";

            HttpWebResponse priceResponse = (HttpWebResponse)request.GetResponse();

            Console.WriteLine("Connection: " + priceResponse.StatusCode);

            string priceJsonString;
            using (Stream stream = priceResponse.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                priceJsonString = reader.ReadToEnd();
            }

            priceJsonString = priceJsonString[8..];
            priceJsonString = priceJsonString.Remove(priceJsonString.Length - 1);

            priceJsonString = priceJsonString.Insert(0, "[");
            priceJsonString = priceJsonString.Insert(priceJsonString.Length, "]");
            
            JArray priceObjects = JArray.Parse(priceJsonString); // parse as array  

            foreach(var jToken in priceObjects)
            {
                var root = (JObject) jToken;
                foreach((string itemId, var value) in root)
                {
                    if (!InfoDict.TryGetValue(int.Parse(itemId), out ItemInfo itemInfo)) continue;
                    
                    if(double.TryParse((string) value["highTime"] ?? string.Empty, out double unixH))
                    {
                        DateTime highTime = UnixTimeStampToDateTime(unixH);
                        
                        itemInfo.BuyTime = highTime;
                    }

                    if (double.TryParse((string) value["lowTime"] ?? string.Empty, out double unixL))
                    {
                        DateTime lowTime = UnixTimeStampToDateTime(unixL);
                        
                        itemInfo.SellTime = lowTime;
                    }
                    
                    string high = (string)value["high"];
                    itemInfo.CurrentBuy = high;
                    
                    string low = (string)value["low"];
                    itemInfo.CurrentSell = low;
                }
            }
            
            Console.WriteLine("Done!");
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
            return dateTime;
        }
    }
}