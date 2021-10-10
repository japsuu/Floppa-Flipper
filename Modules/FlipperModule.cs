using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FloppaFlipper.Datasets;
using FloppaFlipper.Handlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuickChart;

namespace FloppaFlipper.Modules
{
    public class FlipperModule : ModuleBase<SocketCommandContext>
    {
        /*
        private static readonly Dictionary<int, ItemDataSet> InfoDict = new();
        private static List<ItemDataSet> infoList = new();

        //public static readonly Dictionary<SocketUser, PriceNotificationDataSet> OngoingPriceNotifications = new();

        public static readonly Timer Timer = new();
        
        [Command("embed")]
        public async Task SendRichEmbedAsync()
        {
            EmbedBuilder embed = new();
            
            embed
                .WithTitle("**TEST** | This is a test!")
                
                .WithDescription(
                    "[Wiki](https://test.com) | [prices.runescape.wiki](https://prices.runescape.wiki/osrs/item/64) | [GE-tracker](https://www.ge-tracker.com/item/64)")
                
                .WithThumbnailUrl(IconsApiEndpoint + 64)
                
                .WithColor(Color.Gold)
                
                .WithFooter(footer => footer.Text = "Flip fo no hoe")
                
                .WithCurrentTimestamp();
            
            
            var textChannel = (SocketTextChannel)Context.Channel;
            var thread = await textChannel.CreateThreadAsync("Test thread!");

            await thread.SendMessageAsync("Test test, can you hear me?");

            await thread.SendMessageAsync(embed: embed.Build());
        }

        [Command("start")]
        public async Task Start()
        {
            FetchItemMappings();
            
            Timer.Interval = Program.RefreshRate * 1000;
            Timer.Elapsed += TimerTick;
            Timer.Start();
            
            TimerTick(null, null);
        }
        
        private async void TimerTick(object sender, EventArgs e)
        {
            Timer.Enabled = false;
            
            FetchItemPrices();

            await ShowChangedPrices(20000, 20);
            
            Timer.Enabled = true;
        }
        
        [Command("show")]
        [Summary("Show the items with changed margins.")]
        public async Task ShowChangedPrices(int amount = 0, double minChangePercentage = 5)
        {
            //TODO: When first time checking the prices, create a list with all the valid items that we should check, and just update them?
            
            //TODO: Create separate methods for Buy and Sell price checks/dips:
            //TODO: Price dip contains Buy price, it's drop, etc.   Price rise contains Sell price, it's increase, etc.
            
            //TODO: Automatically execute this method after the price checks.
            
            //TODO: Create a blacklist system.
            
            //TODO: Separate the members and non-members items.
            
            //TODO: For accepted items get their price history at https://prices.runescape.wiki/api/v1/osrs/timeseries?timestep=5m&id=4151
            //TODO: and generate a chart for them at https://quickchart.io/documentation/sparkline-api/ .
            
            // Only execute for items we would like to potentially flip. Skip the ones with not enough price data.
            foreach (ItemDataSet item in infoList.Take(amount))
            {
                // Check if we have got enough info of the prices of the item
                if(item._24hAverage == null) continue;
                if(item._6hAverage == null) continue;
                if(item._1hAverage == null) continue;
                
                // Check that the item price is great enough
                if(!long.TryParse(item._24hAverage.AvgBuyPrice, out long price) || price < Program.MinBuyPrice) continue;
                
                // Check if the item has not raised a notification in a while
                if(DateTime.Now.Subtract(item.TimeLastNotified).TotalMinutes < Program.ItemNotificationCooldown) continue;
                
                // Check if it's a dip
                if(item.GetChangePercentage(true) > 0) continue;
                
                // Check that the item's change percentage is great enough
                if(Math.Abs(item.GetChangePercentage(true)) < minChangePercentage) continue;
                
                // Check that the item has been traded in the last hour
                if(!long.TryParse(item._1hAverage.BuyPriceVolume, out long result) || result < 3) continue;
                
                // Check that the item has great enough volume
                if(!long.TryParse(item._24hAverage.BuyPriceVolume, out long volume) || volume < Program.MinTradedVolume) continue;
                
                // Get the 5m time series for this item
                string timeSeriesJson = FetchPriceJson(TimeSeriesApiEndpoint, item.Id);
                
                List<TimeSeriesDataSet> dataSets = JsonConvert.DeserializeObject<List<TimeSeriesDataSet>>(timeSeriesJson);

                #region Building the sparkline

                if (dataSets == null)
                {
                    Console.WriteLine("Could not create a dataset for the graph.");
                    continue;
                }

                string labelsString = "";
                string dataString = "";
                // Only select the last 72 items, to match the 6 hour info
                foreach (TimeSeriesDataSet dataSet in dataSets.Skip(Math.Max(0, dataSets.Count - 72)))
                {
                    if (dataSet.AvgHighPrice == null)
                    {
                        labelsString += $"'{UnixTimeStampToDateTime(dataSet.Timestamp).ToLongTimeString()}'";
                        dataString += "null";
                    }
                    else
                    {
                        labelsString += $"'{UnixTimeStampToDateTime(dataSet.Timestamp).ToLongTimeString()}'";
                        dataString += dataSet.AvgHighPrice;
                    }
                    
                    if(dataString.Length > Program.MaxSparklineDatasetLength || labelsString.Length > Program.MaxSparklineDatasetLength)
                        break;

                    labelsString += ",";
                    dataString += ",";
                }

                Chart qc = new Chart();

                qc.Width = 300;
                qc.Height = 250;
                qc.Config = @"
{
    type: 'line',
  data: {
    labels:[" + labelsString + @"],
    datasets: [{
      label: 'Buy price',
      pointRadius: 1.5,
      borderWidth: 1,
      lineTension: 0.2,
      spanGaps: 'true',
      backgroundColor: 'rgba(255, 0, 0, 0.2)',
      borderColor: 'red',
      data: [" + dataString + @"]
    }]
  }
}";

                #endregion
                
                // Build the embed
                EmbedBuilder embed = new();
                ComponentBuilder comp = new();
                comp.WithSelectMenu(
                    new SelectMenuBuilder()
                    {
                        CustomId = "NotifyMeSelectMenu",
                        Disabled = false,
                        Placeholder = "Select when to notify if the item recovers:",
                        MinValues = 1,
                        MaxValues = 1,
                        Options = new List<SelectMenuOptionBuilder>()
                        {
                            new("1h average", "when the item reaches the 1h average price"),
                            new("6h average", "when the item reaches the 6h average price"),
                            new("12h average", "when the item reaches the 12h average price")
                        }
                    }
                );

                string change = "dropped";
                
                if (double.Parse(item.LatestBuy) > double.Parse(item._6hAverage.AvgBuyPrice)) change = "increased";
                
                
                embed
                    .WithTitle($"**{item.Name}** | potential dip!")
                
                    .WithDescription(
                        $"[Wiki]({item.WikiLink}) | [prices.runescape.wiki]({PriceInfoPageApiEndpoint + item.Id}) | [GE-tracker]({GeTrackerPageApiEndpoint + item.Id})")
                
                    .WithThumbnailUrl(item.IconLink)
                
                    .WithColor(GetColorByPercent(item.GetChangePercentage(true)))
                
                    .WithFooter(footer => footer.Text = "Flip fo no hoe")
                
                    .WithCurrentTimestamp();

                
                embed
                    .AddField("Today's **buy** volume: " + item._24hAverage.BuyPriceVolume,
                        "Last hour: " + item._1hAverage.BuyPriceVolume);

                
                embed
                    .AddField("Today's **sell** volume: " + item._24hAverage.SellPriceVolume,
                        "Last hour: " + item._1hAverage.SellPriceVolume);
                

                embed
                    .AddField(Emote.Parse("<:Buy:894836544442093568>") + $"**Buy price** `has {change} {item.GetChangePercentage(true):F2}%`:",
                        $"\tLast 6h's average `{item._6hAverage.AvgBuyPrice}` dropped to `{item.LatestBuy}`");

                
                embed
                    .AddField(Emote.Parse("<:Sell:894836591590256660>") + $"**Sell price** `has {change} {item.GetChangePercentage(false):F2}%`:",
                        $"\tLast 6h's average `{item._6hAverage.AvgSellPrice}` dropped to `{item.LatestSell}`");


                embed
                    .WithImageUrl(qc.GetShortUrl());
                
                
                item.TimeLastNotified = DateTime.Now;

                await ReplyAsync(embed: embed.Build());

                await ReplyAsync("🔔 ding dong bing bong 🔔", component: comp.Build());
            }

            Emoji tUp = new("👍");

            if (amount >= 1)
            {
                await Context.Message.AddReactionAsync(tUp);
                return;
            }
            
            await ReplyAsync("Invalid count", false, null, null, null, Context.Message.Reference);
                
            tUp = new Emoji("❌");

            await Context.Message.AddReactionAsync(tUp);
        }

        private static void FetchItemMappings()
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
            
            infoList = JsonConvert.DeserializeObject<List<ItemDataSet>>(infoJsonString);
            
            if (infoList == null) return;
            
            foreach (ItemDataSet info in infoList)
            {
                InfoDict.Add(info.Id, info);    //BUG: System.ArgumentException: An item with the same key has already been added. Key: 10344.
            }
            
            Console.WriteLine("Done!\n");
        }

        private static void FetchItemPrices()
        {
            // Get the latest prices for all items
            Console.WriteLine("Fetching prices...");
            string latestPricesJson = FetchPriceJson(ConfigHandler.Config.LatestPricesApiEndpoint, -1);
            
            // Get the 1h prices for all items
            string _5mPricesJson = FetchPriceJson(ConfigHandler.Config._5MinPricesApiEndpoint, -1);
            
            // Get the 1h prices for all items
            string _1hPricesJson = FetchPriceJson(ConfigHandler.Config._1HourPricesApiEndpoint, -1);
            
            // Get the 6h prices for all items
            string _6hPricesJson = FetchPriceJson(ConfigHandler.Config._6HourPricesApiEndpoint, -1);
            
            // Get the 24h prices for all items
            string _24hPricesJson = FetchPriceJson(ConfigHandler.Config._24HourPricesApiEndpoint, -1);
            
            JArray latestPriceObjects = JArray.Parse(latestPricesJson);
            JArray _5mPriceObjects = JArray.Parse(_5mPricesJson);
            JArray _1hPriceObjects = JArray.Parse(_1hPricesJson);
            JArray _6hPriceObjects = JArray.Parse(_6hPricesJson);
            JArray _24hPriceObjects = JArray.Parse(_24hPricesJson);

            foreach (JToken t in latestPriceObjects)
            {
                // Parse the root object (High, Low, HighTime, LowTime)
                JObject latestObject = (JObject)t;
                foreach((string itemId, var value) in latestObject)
                {
                    if (!InfoDict.TryGetValue(int.Parse(itemId), out ItemDataSet itemInfo)) continue;
                    
                    if(long.TryParse((string) value["highTime"] ?? string.Empty, out long unixH))   //NOTE: Changed from doubles to longs
                    {
                        DateTime highTime = UnixTimeStampToDateTime(unixH);
                        
                        itemInfo.LatestBuyTime = highTime;
                    }

                    if (long.TryParse((string) value["lowTime"] ?? string.Empty, out long unixL))   //NOTE: Changed from doubles to longs
                    {
                        DateTime lowTime = UnixTimeStampToDateTime(unixL);
                        
                        itemInfo.LatestSellTime = lowTime;
                    }
                    
                    string high = (string)value["high"];
                    itemInfo.LatestBuy = high;
                    
                    string low = (string)value["low"];
                    itemInfo.LatestSell = low;
                }
            }

            foreach (JToken t in _5mPriceObjects)
            {
                // Parse the 1h object
                JObject _5mObject = (JObject)t;
                foreach((string itemId, var value) in _5mObject)
                {
                    PriceAverageDataSet datedInfo = new();
                    
                    string avgHigh = (string)value["avgHighPrice"];
                    datedInfo.AvgBuyPrice = avgHigh;
                    
                    string highVol = (string)value["highPriceVolume"];
                    datedInfo.BuyPriceVolume = highVol;
                    
                    string avgLow = (string)value["avgLowPrice"];
                    datedInfo.AvgSellPrice = avgLow;
                    
                    string lowVol = (string)value["lowPriceVolume"];
                    datedInfo.SellPriceVolume = lowVol;
                    
                    InfoDict[int.Parse(itemId)]._5mAverage = datedInfo;
                }
            }

            foreach (JToken t in _1hPriceObjects)
            {
                // Parse the 1h object
                JObject _1hObject = (JObject)t;
                foreach((string itemId, var value) in _1hObject)
                {
                    PriceAverageDataSet datedInfo = new();
                    
                    string avgHigh = (string)value["avgHighPrice"];
                    datedInfo.AvgBuyPrice = avgHigh;
                    
                    string highVol = (string)value["highPriceVolume"];
                    datedInfo.BuyPriceVolume = highVol;
                    
                    string avgLow = (string)value["avgLowPrice"];
                    datedInfo.AvgSellPrice = avgLow;
                    
                    string lowVol = (string)value["lowPriceVolume"];
                    datedInfo.SellPriceVolume = lowVol;
                    
                    InfoDict[int.Parse(itemId)]._1hAverage = datedInfo;
                }
            }

            foreach (JToken t in _6hPriceObjects)
            {
                // Parse the 6h object
                JObject _6hObject = (JObject)t;
                foreach((string itemId, var value) in _6hObject)
                {
                    PriceAverageDataSet datedInfo = new();
                    
                    string avgHigh = (string)value["avgHighPrice"];
                    datedInfo.AvgBuyPrice = avgHigh;
                    
                    string highVol = (string)value["highPriceVolume"];
                    datedInfo.BuyPriceVolume = highVol;
                    
                    string avgLow = (string)value["avgLowPrice"];
                    datedInfo.AvgSellPrice = avgLow;
                    
                    string lowVol = (string)value["lowPriceVolume"];
                    datedInfo.SellPriceVolume = lowVol;
                    
                    InfoDict[int.Parse(itemId)]._6hAverage = datedInfo;
                }
            }

            foreach (JToken t in _24hPriceObjects)
            {
                // Parse the 24h object
                JObject _24hObject = (JObject)t;
                foreach((string itemId, var value) in _24hObject)
                {
                    PriceAverageDataSet datedInfo = new();
                    
                    string avgHigh = (string)value["avgHighPrice"];
                    datedInfo.AvgBuyPrice = avgHigh;
                    
                    string highVol = (string)value["highPriceVolume"];
                    datedInfo.BuyPriceVolume = highVol;
                    
                    string avgLow = (string)value["avgLowPrice"];
                    datedInfo.AvgSellPrice = avgLow;
                    
                    string lowVol = (string)value["lowPriceVolume"];
                    datedInfo.SellPriceVolume = lowVol;
                    
                    InfoDict[int.Parse(itemId)]._24hAverage = datedInfo;
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

            if (endpoint ==  ConfigHandler.Config._5MinPricesApiEndpoint ||
                endpoint ==  ConfigHandler.Config._1HourPricesApiEndpoint ||
                endpoint ==  ConfigHandler.Config._6HourPricesApiEndpoint ||
                endpoint ==  ConfigHandler.Config._24HourPricesApiEndpoint)
            {
                json = json[..json.LastIndexOf(',')];
                json = json.Insert(json.Length, "}");

                json = json[8..];
                json = json.Remove(json.Length - 1);

                json = json.Insert(0, "[");
                json = json.Insert(json.Length, "]");
            }
            else if (endpoint == ConfigHandler.Config.LatestPricesApiEndpoint)
            {
                json = json[8..];
                json = json.Remove(json.Length - 1);

                json = json.Insert(0, "[");
                json = json.Insert(json.Length, "]");
            }
            else if (endpoint == ConfigHandler.Config.TimeSeriesApiEndpoint)
            {
                json = json[8..];
                json = json[..json.LastIndexOf(',')];
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
            Color color = Math.Abs(percent) switch
            {
                < 5 => Color.Blue,
                < 10 => Color.LightOrange,
                < 20 => Color.Orange,
                > 20 => Color.Red,
                _ => Color.Default
            };

            return color;
        }
        
        */
    }
}