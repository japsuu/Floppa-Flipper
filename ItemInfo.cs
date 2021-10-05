using System;
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

        private string name;
        [JsonProperty("name")]
        public string Name
        {
            get => name;
            set
            {
                name = value;
                WikiPage = (FlipperModule.WikiPageApiEndpoint + value).Replace(' ', '_');
            }
            
        }

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

        public DateTime LatestBuyTime;
        public DateTime LatestSellTime;

        public override string ToString()
        {
            return $"{Name}: CB: {CurrentBuy}, CS: {CurrentSell}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>How many percents the item's value has changed since the last check.</returns>
        public double GetChangePercentage()
        {
            return -ChangePercentage(long.Parse(currentBuy), long.Parse(PreviousBuy));
        }

        private static double ChangePercentage(long currentVal, long latestVal)
        {
            return 100 * (latestVal - currentVal) / (double)Math.Abs(currentVal);
        }
    }
}