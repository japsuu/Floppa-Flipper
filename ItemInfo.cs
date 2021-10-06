using System;
using System.Collections.Generic;
using FloppaFlipper.Modules;
using Newtonsoft.Json;

namespace FloppaFlipper
{
    public class ItemInfo
    {
        [JsonProperty("examine")]
        public string Examine { get; set; }

        private int id;
        [JsonProperty("id")]
        public int Id
        {
            get => id;
            set
            {
                id = value;
                Icon = FlipperModule.IconsApiEndpoint + value;
                WikiPage = FlipperModule.WikiApiEndpoint + value;
            }
        }
        
        public string WikiPage { get; set; }

        [JsonProperty("members")]
        public bool Members { get; set; }

        [JsonProperty("lowalch")]
        public int LowAlch { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("highalch")]
        public int HighAlch { get; set; }
        
        public string Icon { get; private set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [Obsolete("Use DatedItemInfo.")]
        public string PreviousBuy = "none";
        
        private string currentBuy = "none";
        public string CurrentBuy
        {
            get => currentBuy;
            set
            {
                if (currentBuy == "none")
                    PreviousBuy = value;
                else
                    PreviousBuy = currentBuy;
                currentBuy = value;
            }
        }
        
        [Obsolete("Use DatedItemInfo.")]
        public string PreviousSell = "none";
        
        private string currentSell = "none";
        public string CurrentSell
        {
            get => currentSell;
            set
            {
                if (currentSell == "none")
                    PreviousSell = value;
                else
                    PreviousSell = currentSell;
                currentSell = value;
            }
        }

        public DateTime LatestAvailableBuyTime;
        public DateTime LatestAvailableSellTime;

        public DatedItemInfo _1hInfo { get; set; }
        public DatedItemInfo _6hInfo { get; set; }
        public DatedItemInfo _24hInfo { get; set; }

        public DateTime TimeLastNotified;

        public override string ToString()
        {
            return $"{Name}: CB: {CurrentBuy}, CS: {CurrentSell}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>How many percents the item's value has changed since the last check.</returns>
        public double GetChangePercentage(bool isBuy)
        {
            if (isBuy)
            {
                if (!long.TryParse(currentBuy, out long resultCur)) return 0;
                if (!long.TryParse(_6hInfo.AvgBuyPrice, out long result1H)) return 0;
            
                return ChangePercentage(result1H, resultCur);
            }
            else
            {
                if (!long.TryParse(currentSell, out long resultCur)) return 0;
                if (!long.TryParse(_6hInfo.AvgSellPrice, out long result1H)) return 0;
            
                return ChangePercentage(result1H, resultCur);
            }
        }

        private static double ChangePercentage(long oldVal, long newVal)
        {
            return 100 * (newVal - oldVal) / (double)Math.Abs(oldVal);
        }
    }

    public class DatedItemInfo
    {
        private string avgBuyPrice;
        public string AvgBuyPrice
        {
            get => string.IsNullOrEmpty(avgBuyPrice) ? "not available" : avgBuyPrice;
            set => avgBuyPrice = value;
        }

        private string buyPriceVolume;
        public string BuyPriceVolume
        {
            get => string.IsNullOrEmpty(buyPriceVolume) ? "not available" : buyPriceVolume;
            set => buyPriceVolume = value;
        }
        
        private string avgSellPrice;
        public string AvgSellPrice
        {
            get => string.IsNullOrEmpty(avgSellPrice) ? "not available" : avgSellPrice;
            set => avgSellPrice = value;
        }

        private string sellPriceVolume;
        public string SellPriceVolume
        {
            get => string.IsNullOrEmpty(sellPriceVolume) ? "not available" : sellPriceVolume;
            set => sellPriceVolume = value;
        }
    }

    public class TimeSeriesDataSet
    {
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("avgHighPrice")]
        public int? AvgHighPrice { get; set; }

        [JsonProperty("avgLowPrice")]
        public int? AvgLowPrice { get; set; }

        [JsonProperty("highPriceVolume")]
        public int? HighPriceVolume { get; set; }

        [JsonProperty("lowPriceVolume")]
        public int? LowPriceVolume { get; set; }
    }

    public class PriceNotificationData
    {
        public ItemInfo ItemToTrack;
        public long PriceToNotifyAt;
    }
}