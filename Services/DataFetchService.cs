using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FloppaFlipper.Datasets;
using FloppaFlipper.Handlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FloppaFlipper.Services
{
    public class DataFetchService
    {
        private readonly LoggingService loggingService;
        
        private readonly HashSet<uint> idBlacklist;
        
        /// <summary>
        /// Key = item ID, Value = itemData. Does not contain any blacklisted items.
        /// </summary>
        public readonly Dictionary<uint, ItemDataSet> ItemDataDict = new();
        
        public DataFetchService(LoggingService loggingService)
        {
            this.loggingService = loggingService;

            idBlacklist = ConfigHandler.Config.BlacklistedItemIds != null
                ? new HashSet<uint>(ConfigHandler.Config.BlacklistedItemIds)
                : new HashSet<uint>();
            
            Console.WriteLine("[DATA FETCH SERVICE STARTED]");
        }

        public async Task FetchItemMappings()
        {
            Console.WriteLine("Fetching item mappings...");

            // Get the JSON as a string
            string infoJsonString = await JsonHandler.FetchJsonFromEndpoint(ConfigHandler.Config.MappingApiEndpoint);

            if (infoJsonString == null) return;
            
            // Deserialize the JSON to an array of data objects
            List<ItemDataSet> itemDataSets = JsonConvert.DeserializeObject<List<ItemDataSet>>(infoJsonString);
            
            if (itemDataSets == null || itemDataSets.Count < 1)
            {
                await loggingService.LogError("Fetched item mappings json was empty!");
                
                return;
            }
            
            // Add the data objects to the dictionary
            foreach (ItemDataSet data in itemDataSets.Where(data => !idBlacklist.Contains(data.Id)))
            {
                ItemDataDict.Add(data.Id, data);
            }
            
            Console.WriteLine($"Successfully fetched the data of {ItemDataDict.Count} items.\n");
        }

        /// <summary>
        /// Updates the prices of all non-blacklisted items.
        /// </summary>
        public async Task UpdateItemPrices()
        {
            // Get the latest prices for all items
            string latestPricesJson = await JsonHandler.FetchPriceJson(ConfigHandler.Config.LatestPricesApiEndpoint);
            
            // Get the 1h prices for all items
            string _5mPricesJson = await JsonHandler.FetchPriceJson(ConfigHandler.Config._5MinPricesApiEndpoint);
            
            // Get the 1h prices for all items
            string _1hPricesJson = await JsonHandler.FetchPriceJson(ConfigHandler.Config._1HourPricesApiEndpoint);
            
            // Get the 6h prices for all items
            string _6hPricesJson = await JsonHandler.FetchPriceJson(ConfigHandler.Config._6HourPricesApiEndpoint);
            
            // Get the 24h prices for all items
            string _24hPricesJson = await JsonHandler.FetchPriceJson(ConfigHandler.Config._24HourPricesApiEndpoint);
            
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
                    // Check if we are tracking the price of this item
                    if (!ItemDataDict.TryGetValue(uint.Parse(itemId), out ItemDataSet itemData)) continue;
                    
                    if(long.TryParse((string) value["highTime"] ?? string.Empty, out long unixH))
                    {
                        DateTime highTime = Helpers.UnixTimeStampToDateTime(unixH);
                        
                        itemData.LatestBuyTime = highTime;
                    }

                    if (long.TryParse((string) value["lowTime"] ?? string.Empty, out long unixL))
                    {
                        DateTime lowTime = Helpers.UnixTimeStampToDateTime(unixL);
                        
                        itemData.LatestSellTime = lowTime;
                    }
                    
                    string high = (string)value["high"];
                    itemData.LatestBuy = high;
                    
                    string low = (string)value["low"];
                    itemData.LatestSell = low;
                }
            }

            foreach (JToken t in _5mPriceObjects)
            {
                // Parse the 1h object
                JObject _5mObject = (JObject)t;
                foreach((string itemId, var value) in _5mObject)
                {
                    // Check if we are tracking the price of this item
                    if (!ItemDataDict.TryGetValue(uint.Parse(itemId), out ItemDataSet itemData)) continue;

                    PriceAverageDataSet datedInfo = new();
                    
                    string avgHigh = (string)value["avgHighPrice"];
                    datedInfo.AvgBuyPrice = avgHigh;
                    
                    string highVol = (string)value["highPriceVolume"];
                    datedInfo.BuyPriceVolume = highVol;
                    
                    string avgLow = (string)value["avgLowPrice"];
                    datedInfo.AvgSellPrice = avgLow;
                    
                    string lowVol = (string)value["lowPriceVolume"];
                    datedInfo.SellPriceVolume = lowVol;
                    
                    itemData._5mAverage = datedInfo;
                }
            }

            foreach (JToken t in _1hPriceObjects)
            {
                // Parse the 1h object
                JObject _1hObject = (JObject)t;
                foreach((string itemId, var value) in _1hObject)
                {
                    // Check if we are tracking the price of this item
                    if (!ItemDataDict.TryGetValue(uint.Parse(itemId), out ItemDataSet itemData)) continue;

                    PriceAverageDataSet datedInfo = new();
                    
                    string avgHigh = (string)value["avgHighPrice"];
                    datedInfo.AvgBuyPrice = avgHigh;
                    
                    string highVol = (string)value["highPriceVolume"];
                    datedInfo.BuyPriceVolume = highVol;
                    
                    string avgLow = (string)value["avgLowPrice"];
                    datedInfo.AvgSellPrice = avgLow;
                    
                    string lowVol = (string)value["lowPriceVolume"];
                    datedInfo.SellPriceVolume = lowVol;
                    
                    itemData._1hAverage = datedInfo;
                }
            }

            foreach (JToken t in _6hPriceObjects)
            {
                // Parse the 6h object
                JObject _6hObject = (JObject)t;
                foreach((string itemId, var value) in _6hObject)
                {
                    // Check if we are tracking the price of this item
                    if (!ItemDataDict.TryGetValue(uint.Parse(itemId), out ItemDataSet itemData)) continue;

                    PriceAverageDataSet datedInfo = new();
                    
                    string avgHigh = (string)value["avgHighPrice"];
                    datedInfo.AvgBuyPrice = avgHigh;
                    
                    string highVol = (string)value["highPriceVolume"];
                    datedInfo.BuyPriceVolume = highVol;
                    
                    string avgLow = (string)value["avgLowPrice"];
                    datedInfo.AvgSellPrice = avgLow;
                    
                    string lowVol = (string)value["lowPriceVolume"];
                    datedInfo.SellPriceVolume = lowVol;
                    
                    itemData._6hAverage = datedInfo;
                }
            }

            foreach (JToken t in _24hPriceObjects)
            {
                // Parse the 24h object
                JObject _24hObject = (JObject)t;
                foreach((string itemId, var value) in _24hObject)
                {
                    // Check if we are tracking the price of this item
                    if (!ItemDataDict.TryGetValue(uint.Parse(itemId), out ItemDataSet itemData)) continue;

                    PriceAverageDataSet datedInfo = new();
                    
                    string avgHigh = (string)value["avgHighPrice"];
                    datedInfo.AvgBuyPrice = avgHigh;
                    
                    string highVol = (string)value["highPriceVolume"];
                    datedInfo.BuyPriceVolume = highVol;
                    
                    string avgLow = (string)value["avgLowPrice"];
                    datedInfo.AvgSellPrice = avgLow;
                    
                    string lowVol = (string)value["lowPriceVolume"];
                    datedInfo.SellPriceVolume = lowVol;
                    
                    itemData._24hAverage = datedInfo;
                }
            }
            
            Console.WriteLine("Item prices updated.");
        }
    }
}