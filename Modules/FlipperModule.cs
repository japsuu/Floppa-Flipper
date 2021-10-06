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
using QuickChart;

namespace FloppaFlipper.Modules
{
    public class FlipperModule : ModuleBase<SocketCommandContext>
    {
        // Price endpoints
        private const string LatestPricesApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/latest";
        private const string _1HourPricesApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/1h";
        private const string _6HourPricesApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/6h";
        private const string _24HourPricesApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/24h";
        
        // Mapping / Info endpoints
        private const string MappingApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/mapping";
        public const string IconsApiEndpoint = "https://secure.runescape.com/m=itemdb_oldschool/obj_big.gif?id=";
        public const string WikiApiEndpoint = "https://oldschool.runescape.wiki/w/Special:Lookup?type=item&id=";
        
        // Price info endpoints
        private const string PriceInfoPageApiEndpoint = "https://prices.runescape.wiki/osrs/item/";
        private const string GeTrackerPageApiEndpoint = "https://www.ge-tracker.com/item/";
        
        // TimeSeries endpoint
        private const string TimeSeriesApiEndpoint =
            "https://prices.runescape.wiki/api/v1/osrs/timeseries?timestep=5m&id=";
        
        private static readonly Dictionary<int, ItemInfo> InfoDict = new();
        private static List<ItemInfo> infoList = new();

        public static readonly Timer Timer = new();
        
        [Command("embed")]
        public async Task SendRichEmbedAsync()
        {
            EmbedBuilder embed = new();
            
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
            //TODO: Create separate methods for Buy and Sell price checks/dips:
            //TODO: Price dip contains Buy price, it's drop, etc.   Price rise contains Sell price, it's increase, etc.
            
            //TODO: Automatically execute this method after the price checks.
            
            //TODO: Create a blacklist system.
            
            //TODO: Separate the members and non-members items.
            
            //TODO: Only select items that have a high enough volume.
            
            //TODO: For accepted items get their price history at https://prices.runescape.wiki/api/v1/osrs/timeseries?timestep=5m&id=4151
            //TODO: and generate a chart for them at https://quickchart.io/documentation/sparkline-api/ .
            
            // Only execute for items we would like to potentially flip. Skip the ones with not enough price data.
            foreach (ItemInfo item in infoList.Take(amount))
            {
                // Check if we have got enough info of the prices of the item
                if(item._24hInfo == null) continue;
                if(item._6hInfo == null) continue;
                if(item._1hInfo == null) continue;
                if(long.Parse(item._24hInfo.AvgBuyPrice) < 100) continue;
                
                // Check if the item has not raised a notification in a while
                if(DateTime.Now.Subtract(item.TimeLastNotified).TotalMinutes < Program.ItemNotificationCooldown) continue;
                
                // Check if it's a dip
                if(item.GetChangePercentage(true) > 0) continue;
                
                // Check that the item's change percentage is great enough
                if(Math.Abs(item.GetChangePercentage(true)) < minChangePercentage) continue;
                
                // Check that the item has been traded in the last hour
                if(!long.TryParse(item._1hInfo.BuyPriceVolume, out long result) || result < 3) continue;

                #region Building the sparkline

                // Get the 5m time series for this item
                string timeSeriesJson = FetchPriceJson(TimeSeriesApiEndpoint, item.Id);
                
                List<TimeSeriesDataSet> dataSets = JsonConvert.DeserializeObject<List<TimeSeriesDataSet>>(timeSeriesJson);

                if (dataSets == null)
                {
                    Console.WriteLine("Could not create a dataset for the graph.");
                    continue;
                }

                string labelsString = "";
                string dataString = "";
                foreach (TimeSeriesDataSet dataSet in dataSets.Skip(Math.Max(0, dataSets.Count - 300)))
                {
                    if(dataSet.AvgHighPrice == null) continue;

                    labelsString += $"'{UnixTimeStampToDateTime(dataSet.Timestamp).ToLongTimeString()}'";
                    dataString += dataSet.AvgHighPrice;
                    
                    if(dataString.Length > Program.MaxSparklineDatasetLength || labelsString.Length > Program.MaxSparklineDatasetLength)
                        break;

                    labelsString += ",";
                    dataString += ",";
                }

                Chart qc = new Chart();

                qc.Width = 500;
                qc.Height = 300;
                qc.Config = @"
{
    type: 'line',
    width: '20',
    height: '40',
  data: {
    labels:[" + labelsString + @"],
    datasets: [{
      label: 'Buy price',
      pointRadius: 1.5,
      borderWidth: 1,
      lineTension: 0,
      backgroundColor: 'rgba(255, 0, 0, 0.2)',
      borderColor: 'red',
      data: [" + dataString + @"]
    }]
  }
}";

                #endregion
                
                // Build the embed
                EmbedBuilder embed = new();

                string change = "dropped";
                
                if (double.Parse(item.CurrentBuy) > double.Parse(item._6hInfo.AvgBuyPrice)) change = "increased";
                
                
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


                embed
                    .WithImageUrl(qc.GetShortUrl());
                
                item.TimeLastNotified = DateTime.Now;

                ReplyAsync(embed: embed.Build());
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
                InfoDict.Add(info.Id, info);    //BUG: System.ArgumentException: An item with the same key has already been added. Key: 10344.
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
            string latestPricesJson = FetchPriceJson(LatestPricesApiEndpoint, -1);
            
            // Get the 1h prices for all items
            string _1hPricesJson = FetchPriceJson(_1HourPricesApiEndpoint, -1);
            
            // Get the 6h prices for all items
            string _6hPricesJson = FetchPriceJson(_6HourPricesApiEndpoint, -1);
            
            // Get the 24h prices for all items
            string _24hPricesJson = FetchPriceJson(_24HourPricesApiEndpoint, -1);
            
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
                    
                    if(long.TryParse((string) value["highTime"] ?? string.Empty, out long unixH))   //NOTE: Changed from doubles to longs
                    {
                        DateTime highTime = UnixTimeStampToDateTime(unixH);
                        
                        itemInfo.LatestAvailableBuyTime = highTime;
                    }

                    if (long.TryParse((string) value["lowTime"] ?? string.Empty, out long unixL))   //NOTE: Changed from doubles to longs
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
        /// <param name="id">Item id. Set to -1 to disable</param>
        /// <returns>Full json string</returns>
        private static string FetchPriceJson(string endpoint, int id)
        {
            string json;

            string finalEndpoint = endpoint;
            if (id != -1) finalEndpoint += id;
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(finalEndpoint);

            request.UserAgent = "FloppaFlipper - Japsu#8887";
            request.Method = "GET";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            Console.WriteLine("[CONN] Connection: " + response.StatusCode);
            
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new(stream, System.Text.Encoding.UTF8);
                json = reader.ReadToEnd();
            }

            switch (endpoint)
            {
                case _1HourPricesApiEndpoint or _6HourPricesApiEndpoint or _24HourPricesApiEndpoint:
                    Console.WriteLine("_hour");
                    json = json[..json.LastIndexOf(',')];
                    json = json.Insert(json.Length, "}");

                    json = json[8..];
                    json = json.Remove(json.Length - 1);

                    json = json.Insert(0, "[");
                    json = json.Insert(json.Length, "]");
                    break;
                
                case LatestPricesApiEndpoint:
                    Console.WriteLine("latest");
                    json = json[8..];
                    json = json.Remove(json.Length - 1);

                    json = json.Insert(0, "[");
                    json = json.Insert(json.Length, "]");
                    break;
                
                case TimeSeriesApiEndpoint:
                    Console.WriteLine("timeSeries");
                    json = json[8..];
                    json = json[..json.LastIndexOf(',')];
                    break;
            }

            return json;
        }

        private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
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