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
        //TODO: For accepted items get their price history at https://prices.runescape.wiki/api/v1/osrs/timeseries?timestep=5m&id=4151
        //TODO: and generate a chart for them at https://quickchart.io/documentation/sparkline-api/
        
        // Price endpoints
        private const string LatestPricesApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/latest";
        private const string _1HourPricesApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/1h";
        private const string _6HourPricesApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/6h";
        private const string _24HourPricesApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/24h";
        
        // Mapping / Info endpoints
        private const string MappingApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/mapping";
        public const string IconsApiEndpoint = "https://secure.runescape.com/m=itemdb_oldschool/obj_big.gif?id=";
        public const string WikiApiEndpoint = "https://oldschool.runescape.wiki/w/Special:Lookup?type=item&id=";
        public const string PriceInfoPageApiEndpoint = "https://prices.runescape.wiki/osrs/item/";
        public const string GeTrackerPageApiEndpoint = "https://www.ge-tracker.com/item/";
        //public const string WikiPageApiEndpoint = "https://oldschool.runescape.wiki/w/";
        
        private static readonly Dictionary<int, ItemInfo> InfoDict = new();
        private static List<ItemInfo> infoList = new();

        public static readonly Timer Timer = new();
        
        [Command("embed")]
        public async Task SendRichEmbedAsync()
        {
            EmbedBuilder embed = new();
            
            //TODO: Also show 1h prices, and 12h prices. Perform the dip check on those.
            //TODO: Create a blacklist system
            
            embed
                .WithTitle("**Item Name** | potential dip!")
                
                .WithDescription(
                    "[Wiki](https://test.com) | [prices.runescape.wiki](https://prices.runescape.wiki/osrs/item/64) | [GE-tracker](https://www.ge-tracker.com/item/64)")
                
                .WithThumbnailUrl(IconsApiEndpoint + 64)
                
                .WithColor(GetColorByPercent(18.12))
                
                .WithFooter(footer => footer.Text = "Flip fo no hoe")
                
                .WithCurrentTimestamp();


            embed
                .AddField(Emote.Parse("<:Buy:894836544442093568>") + "**Buy price** `has changed 18.12%`:", "\tChanged from `123` to `234`");


            embed
                .AddField(Emote.Parse("<:Sell:894836591590256660>") + "**Sell price** `has changed 18.12%`:", "\tChanged from `123` to `234`");

            //Your embed needs to be built before it is able to be sent
            await ReplyAsync(embed: embed.Build());
        }
        
        [Command("show")]
        [Summary("Show the items with changed margins.")]
        public Task ShowChangedPrices(int amount = 0, double minChangePercentage = 5)
        {
            // Only execute for items we would like to potentially flip. Skip the ones with not enough price data.
            foreach (ItemInfo item in infoList.Take(amount))
            {
                // Check if we have got enough info of the prices of the item
                if(item._24hInfo == null) continue;
                if(item._6hInfo == null) continue;
                if(item._1hInfo == null) continue;
                
                // Check if the item has not raised a notification in a while
                if(DateTime.Now.Subtract(item.TimeLastNotified).TotalMinutes < Program.ItemNotificationCooldown) continue;
                // Check if it's a dip
                if(item.GetChangePercentage(true) > 0) continue;
                
                // Check that the item's change percentage is great enough
                if(Math.Abs(item.GetChangePercentage(true)) < minChangePercentage) continue;
                
                // Build the embed
                EmbedBuilder embed = new();

                string change = "dropped";
                
                if (double.Parse(item.CurrentBuy) > double.Parse(item._1hInfo.AvgBuyPrice)) change = "increased";
                
                embed
                    .WithTitle($"**{item.Name}** | potential dip!")
                
                    .WithDescription(
                        $"[Wiki]({item.WikiPage}) | [prices.runescape.wiki]({PriceInfoPageApiEndpoint + item.Id}) | [GE-tracker]({GeTrackerPageApiEndpoint + item.Id})")
                
                    .WithThumbnailUrl(IconsApiEndpoint + item.Id)
                
                    .WithColor(GetColorByPercent(item.GetChangePercentage(true)))
                
                    .WithFooter(footer => footer.Text = "Flip fo no hoe")
                
                    .WithCurrentTimestamp();

                embed
                    .AddField("Today's **buy** volume: " + item._24hInfo.BuyPriceVolume,
                        "Last hour: " + item._1hInfo.BuyPriceVolume);

                embed
                    .AddField("Today's **sell** volume: " + item._24hInfo.SellPriceVolume,
                        "Last hour: " + item._1hInfo.SellPriceVolume);
                

                embed
                    .AddField(Emote.Parse("<:Buy:894836544442093568>") + $"**Buy price** `has {change} {item.GetChangePercentage(true):F2}%`:",
                        $"\tLast 6h's average `{item._6hInfo.AvgBuyPrice}` dropped to `{item.CurrentBuy}`");

                

                embed
                    .AddField(Emote.Parse("<:Sell:894836591590256660>") + $"**Sell price** `has {change} {item.GetChangePercentage(false):F2}%`:",
                        $"\tLast 6h's average `{item._6hInfo.AvgSellPrice}` dropped to `{item.CurrentSell}`");
                
                item.TimeLastNotified = DateTime.Now;

                ReplyAsync(embed: embed.Build());

                /*
                embed.AddField($"{change} from {item._1hInfo.AvgBuyPrice} to {item.CurrentBuy}",
                        $"[Wiki]({item.WikiPage})")
                    .WithFooter(footer => footer.Text = "Flip fo no hoe")
                    .WithColor(Color.Blue)
                    .WithTitle(item.Name)
                    .WithDescription($"price has changed {item.GetChangePercentage():F2}%")
                    .WithCurrentTimestamp()
                    .WithThumbnailUrl(item.Icon);
                
                item.TimeLastNotified = DateTime.Now;
                
                ReplyAsync(embed: embed.Build());*/
            }

            Emoji tUp = new("👍");

            if (amount >= 1) return Context.Message.AddReactionAsync(tUp);
            
            ReplyAsync("Invalid count", false, null, null, null, Context.Message.Reference);
                
            tUp = new Emoji("❌");


            return Context.Message.AddReactionAsync(tUp);
        }
        
        public static void FetchItemMappings()
        {
            Console.WriteLine("Querying item data...");
            
            // Connect to the item info API...
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(MappingApiEndpoint);
            request.UserAgent = "FloppaFlipper - Japsu#8887";
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            
            Console.WriteLine("[CONN] Connection: " + response.StatusCode);

            string infoJsonString;
            using (Stream stream = response.GetResponseStream())
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
            // Get the latest prices for all items
            Console.WriteLine("Fetching...");
            string latestPricesJson = FetchPriceJson(LatestPricesApiEndpoint);
            
            // Get the 1h prices for all items
            string _1hPricesJson = FetchPriceJson(_1HourPricesApiEndpoint);
            
            // Get the 6h prices for all items
            string _6hPricesJson = FetchPriceJson(_6HourPricesApiEndpoint);
            
            // Get the 24h prices for all items
            string _24hPricesJson = FetchPriceJson(_24HourPricesApiEndpoint);
            
            JArray latestPriceObjects = JArray.Parse(latestPricesJson);
            JArray _1hPriceObjects = JArray.Parse(_1hPricesJson);
            JArray _6hPriceObjects = JArray.Parse(_6hPricesJson);
            JArray _24hPriceObjects = JArray.Parse(_24hPricesJson);

            foreach (JToken t in latestPriceObjects)
            {
                // Parse the root object (High, Low, HighTime, LowTime)
                JObject latestObject = (JObject)t;
                foreach((string itemId, var value) in latestObject)
                {
                    if (!InfoDict.TryGetValue(int.Parse(itemId), out ItemInfo itemInfo)) continue;
                    
                    if(double.TryParse((string) value["highTime"] ?? string.Empty, out double unixH))
                    {
                        DateTime highTime = UnixTimeStampToDateTime(unixH);
                        
                        itemInfo.LatestAvailableBuyTime = highTime;
                    }

                    if (double.TryParse((string) value["lowTime"] ?? string.Empty, out double unixL))
                    {
                        DateTime lowTime = UnixTimeStampToDateTime(unixL);
                        
                        itemInfo.LatestAvailableSellTime = lowTime;
                    }
                    
                    string high = (string)value["high"];
                    itemInfo.CurrentBuy = high;
                    
                    string low = (string)value["low"];
                    itemInfo.CurrentSell = low;
                }
            }

            foreach (JToken t in _1hPriceObjects)
            {
                // Parse the 1h object
                JObject _1hObject = (JObject)t;
                foreach((string itemId, var value) in _1hObject)
                {
                    DatedItemInfo datedInfo = new();
                    
                    string avgHigh = (string)value["avgHighPrice"];
                    datedInfo.AvgBuyPrice = avgHigh;
                    
                    string highVol = (string)value["highPriceVolume"];
                    datedInfo.BuyPriceVolume = highVol;
                    
                    string avgLow = (string)value["avgLowPrice"];
                    datedInfo.AvgSellPrice = avgLow;
                    
                    string lowVol = (string)value["lowPriceVolume"];
                    datedInfo.SellPriceVolume = lowVol;
                    
                    InfoDict[int.Parse(itemId)]._1hInfo = datedInfo;
                }
            }

            foreach (JToken t in _6hPriceObjects)
            {
                // Parse the 6h object
                JObject _6hObject = (JObject)t;
                foreach((string itemId, var value) in _6hObject)
                {
                    DatedItemInfo datedInfo = new();
                    
                    string avgHigh = (string)value["avgHighPrice"];
                    datedInfo.AvgBuyPrice = avgHigh;
                    
                    string highVol = (string)value["highPriceVolume"];
                    datedInfo.BuyPriceVolume = highVol;
                    
                    string avgLow = (string)value["avgLowPrice"];
                    datedInfo.AvgSellPrice = avgLow;
                    
                    string lowVol = (string)value["lowPriceVolume"];
                    datedInfo.SellPriceVolume = lowVol;
                    
                    InfoDict[int.Parse(itemId)]._6hInfo = datedInfo;
                }
            }

            foreach (JToken t in _24hPriceObjects)
            {
                // Parse the 24h object
                JObject _24hObject = (JObject)t;
                foreach((string itemId, var value) in _24hObject)
                {
                    DatedItemInfo datedInfo = new();
                    
                    string avgHigh = (string)value["avgHighPrice"];
                    datedInfo.AvgBuyPrice = avgHigh;
                    
                    string highVol = (string)value["highPriceVolume"];
                    datedInfo.BuyPriceVolume = highVol;
                    
                    string avgLow = (string)value["avgLowPrice"];
                    datedInfo.AvgSellPrice = avgLow;
                    
                    string lowVol = (string)value["lowPriceVolume"];
                    datedInfo.SellPriceVolume = lowVol;
                    
                    InfoDict[int.Parse(itemId)]._24hInfo = datedInfo;
                }
            }
            
            Console.WriteLine("Done!");
        }

        /// <summary>
        /// Used to fetch json from https://prices.runescape.wiki/api/v1/osrs/ (JSON that starts with '{"data":{"ITEM-ID":'...
        /// </summary>
        /// <param name="endpoint">Endpoint to load the JSON from.</param>
        /// <returns>Full json string</returns>
        private static string FetchPriceJson(string endpoint)
        {
            string json;
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);

            request.UserAgent = "FloppaFlipper - Japsu#8887";
            request.Method = "GET";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            Console.WriteLine("[CONN] Connection: " + response.StatusCode);
            
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new(stream, System.Text.Encoding.UTF8);
                json = reader.ReadToEnd();
            }

            if (endpoint == LatestPricesApiEndpoint)
            {
                json = json[8..];
                json = json.Remove(json.Length - 1);

                json = json.Insert(0, "[");
                json = json.Insert(json.Length, "]");
            }
            else
            {
                json = json[..json.LastIndexOf(',')];
                json = json.Insert(json.Length, "}");
                
                json = json[8..];
                json = json.Remove(json.Length - 1);

                json = json.Insert(0, "[");
                json = json.Insert(json.Length, "]");
            }

            return json;
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
            return dateTime;
        }

        private static Color GetColorByPercent(double percent)
        {
            Color color = Color.Default;

            switch (percent)
            {
                case < 5:
                {
                    color = Color.Blue;
                    break;
                }
                
                case < 10:
                {
                    color = Color.LightOrange;
                    break;
                }
                
                case < 20:
                {
                    color = Color.Orange;
                    break;
                }
                
                case > 20:
                {
                    color = Color.Red;
                    break;
                }
            }

            return color;
        }
    }
}