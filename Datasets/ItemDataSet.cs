using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FloppaFlipper.Handlers;
using Newtonsoft.Json;

namespace FloppaFlipper.Datasets
{
    public class ItemDataSet
    {
        /// <summary>
        /// Unique identifier of this item. Used to configure IconLink and WikiLink.
        /// </summary>
        [JsonProperty("id")]
        public uint Id
        {
            get => id;
            set
            {
                id = value;
                IconLink = ConfigHandler.Config.IconsApiEndpoint + value;
                WikiLink = ConfigHandler.Config.WikiApiEndpoint + value;
            }
        }
        private uint id;

        
        /// <summary>
        /// Name of this item.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
        
        
        /// <summary>
        /// Examine text of this item.
        /// </summary>
        [JsonProperty("examine")]
        public string Examine { get; set; }

        
        /// <summary>
        /// Is members-only item?
        /// </summary>
        [JsonProperty("members")]
        public bool MembersOnly { get; set; }
        
        
        /// <summary>
        /// Link to the wiki-page of this item. 
        /// </summary>
        public string WikiLink { get; private set; }
        
        
        /// <summary>
        /// Link to the icon of this item. 
        /// </summary>
        public string IconLink { get; private set; }

        
        /// <summary>
        /// Shop value of this item.
        /// </summary>
        [JsonProperty("value")]
        public int ShopValue { get; set; }

        
        /// <summary>
        /// HA-value of this item.
        /// </summary>
        [JsonProperty("highalch")]
        public int HighAlchValue { get; set; }

        
        /// <summary>
        /// LA-value of this item.
        /// </summary>
        [JsonProperty("lowalch")]
        public int LowAlchValue { get; set; }

        
        /// <summary>
        /// Buy limit of this item.
        /// </summary>
        [JsonProperty("limit")]
        public int BuyLimit { get; set; }

        
        /// <summary>
        /// Time this item has last raised an price alert.
        /// </summary>
        public DateTime TimeLastNotified;
        
        
        /// <summary>
        /// The timestamp when this item was last bought.
        /// </summary>
        public DateTime LatestBuyTime;
        
        
        /// <summary>
        /// The timestamp when this item was last sold.
        /// </summary>
        public DateTime LatestSellTime;

        
        /// <summary>
        /// Latest price this item was bought at.
        /// </summary>
        public string LatestBuy { get; set; } = "none";
        
        
        /// <summary>
        /// Latest price this item was sold at.
        /// </summary>
        public string LatestSell { get; set; } = "none";

        
        /// <summary>
        /// Price average of the latest 5 minutes.
        /// </summary>
        public PriceAverageDataSet _5mAverage { get; set; }
        
        
        /// <summary>
        /// Price average of the latest 1 hour.
        /// </summary>
        public PriceAverageDataSet _1hAverage { get; set; }
        
        
        /// <summary>
        /// Price average of the latest 6 hours.
        /// </summary>
        public PriceAverageDataSet _6hAverage { get; set; }
        
        
        /// <summary>
        /// Price average of the latest 24 hours.
        /// </summary>
        public PriceAverageDataSet _24hAverage { get; set; }
        
        public List<TimeSeriesDataSet> _5mTimeSeries { get; set; }

        public bool IsFlippable()
        {
            // Check if the item has not raised a notification in a while
            if(DateTime.Now.Subtract(TimeLastNotified).TotalMinutes < ConfigHandler.Config.ItemNotificationCooldown) return false;
            
            // Check if we have got enough info of the prices of the item
            if(_24hAverage == null) return false;
            if(_6hAverage == null) return false;
            if(_1hAverage == null) return false;
            if(_5mAverage == null) return false;

            if (string.IsNullOrEmpty(_5mAverage.AvgSellPrice) || string.IsNullOrEmpty(_5mAverage.AvgBuyPrice) ||
                _5mAverage.AvgBuyPrice == "not available" || _5mAverage.AvgSellPrice == "not available")
                return false;
                
            // Check that the item price is great enough
            if(!long.TryParse(_24hAverage.AvgBuyPrice, out long price) || price < ConfigHandler.Config.MinBuyPrice) return false;
                
            // Check that the item volume is great enough
            if(!long.TryParse(_24hAverage.BuyPriceVolume, out long volume) || volume < ConfigHandler.Config.MinTradedVolume) return false;

            return true;
        }

        private async Task Get5MinTimeSeries()
        {
            // Get the 5m time series for this item
            string timeSeriesJson = await JsonHandler.FetchPriceJson(ConfigHandler.Config.TimeSeriesApiEndpoint, Id);
                
            _5mTimeSeries = JsonConvert.DeserializeObject<List<TimeSeriesDataSet>>(timeSeriesJson);
        }

        public async Task<bool> HasCrashed(double percentage)
        {
            double value = await GetChangePercentage(false);
            
            // Check if it's a dip
            if(value > 0) return false;
                
            // Check that the item's change percentage is great enough
            if(Math.Abs(value) < percentage) return false;

            return true;
        }

        public async Task<bool> HasSpiked(double percentage)
        {
            double value = await GetChangePercentage(true);
            
            // Check if it's a spike
            if(value < 0) return false;
                
            // Check that the item's change percentage is great enough
            if(Math.Abs(value) < percentage) return false;

            return true;
        }

        public override string ToString()
        {
            return $"{Name}: ID: {Id}, 1hAvg: {_1hAverage}";
        }

        // Used to be how many percents the item's 5m average price is relative to the last 6h average price.
        /// <summary>
        /// Percentage of price fluctuation in the last 10m.
        /// </summary>
        /// <returns>How many percents the item's latest 5m average price is relative to the last 10min average price (excluding the latest 5min).</returns>
        /// <param name="isBuy">Set to true if you want the percentage of buy value change, false if sell value change.</param>
        public async Task<double> GetChangePercentage(bool isBuy)
        {
            await Get5MinTimeSeries();
            
            if (isBuy)
            {
                if (_5mTimeSeries[^1].AvgHighPrice == null) return 0;
                if (_5mTimeSeries[^2].AvgHighPrice == null) return 0;
                //if (!long.TryParse(_5mAverage.AvgBuyPrice, out long currentAverage)) return 0;
                //if (!long.TryParse(_6hAverage.AvgBuyPrice, out long oldAverage)) return 0;
            
                return CalculateChangePercentage((int)_5mTimeSeries[^2].AvgHighPrice, (int)_5mTimeSeries[^1].AvgHighPrice);
            }
            else
            {
                if (_5mTimeSeries[^1].AvgLowPrice == null) return 0;
                if (_5mTimeSeries[^2].AvgLowPrice == null) return 0;
                //if (!long.TryParse(_5mAverage.AvgSellPrice, out long currentAverage)) return 0;
                //if (!long.TryParse(_6hAverage.AvgSellPrice, out long oldAverage)) return 0;
            
                return CalculateChangePercentage((int)_5mTimeSeries[^2].AvgLowPrice, (int)_5mTimeSeries[^1].AvgLowPrice);
            }
        }

        private static double CalculateChangePercentage(long oldVal, long newVal)
        {
            return 100 * (newVal - oldVal) / (double)Math.Abs(oldVal);
        }
    }
}