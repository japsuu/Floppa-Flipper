using System;
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

        public bool IsFlippable()
        {
            // Check if the item has not raised a notification in a while
            if(DateTime.Now.Subtract(TimeLastNotified).TotalMinutes < ConfigHandler.Config.ItemNotificationCooldown) return false;
            
            // Check if we have got enough info of the prices of the item
            if(_24hAverage == null) return false;
            if(_6hAverage == null) return false;
            if(_1hAverage == null) return false;
            if(_5mAverage == null) return false;
                
            // Check that the item price is great enough
            if(!long.TryParse(_24hAverage.AvgBuyPrice, out long price) || price < ConfigHandler.Config.MinBuyPrice) return false;
                
            // Check that the item volume is great enough
            if(!long.TryParse(_24hAverage.BuyPriceVolume, out long volume) || volume < ConfigHandler.Config.MinTradedVolume) return false;

            return true;
        }

        public bool HasCrashed(double percentage)
        {
            // Check if it's a dip
            if(GetChangePercentage(false) > 0) return false;
                
            // Check that the item's change percentage is great enough
            if(Math.Abs(GetChangePercentage(false)) < percentage) return false;

            return true;
        }

        public bool HasSpiked(double percentage)
        {
            // Check if it's a spike
            if(GetChangePercentage(false) < 0) return false;
                
            // Check that the item's change percentage is great enough
            if(Math.Abs(GetChangePercentage(false)) < percentage) return false;

            return true;
        }

        public override string ToString()
        {
            return $"{Name}: ID: {Id}, 1hAvg: {_1hAverage}";
        }

        /// <summary>
        /// Percentage of price fluctuation in the last 6h.
        /// </summary>
        /// <returns>How many percents the item's 5m average price is relative to the last 6h average price.</returns>
        /// <param name="isBuy">Set to true if you want the percentage of buy value change, false if sell value change.</param>
        public double GetChangePercentage(bool isBuy)
        {
            if (isBuy)
            {
                if (!long.TryParse(_5mAverage.AvgBuyPrice, out long currentAverage)) return 0;
                if (!long.TryParse(_6hAverage.AvgBuyPrice, out long oldAverage)) return 0;
            
                return CalculateChangePercentage(oldAverage, currentAverage);
            }
            else
            {
                if (!long.TryParse(_5mAverage.AvgSellPrice, out long currentAverage)) return 0;
                if (!long.TryParse(_6hAverage.AvgSellPrice, out long oldAverage)) return 0;
            
                return CalculateChangePercentage(oldAverage, currentAverage);
            }
        }

        private static double CalculateChangePercentage(long oldVal, long newVal)
        {
            return 100 * (newVal - oldVal) / (double)Math.Abs(oldVal);
        }
    }
}