using System.Collections.Generic;

namespace FloppaFlipper.Handlers
{
    public class Config
    {
        public string BotToken;
        public int RefreshRate;
        public int ItemNotificationCooldown;
        public int MaxSparklineDatasetLength;
        public int MinTradedVolume;
        public int MinBuyPrice;
        public double MinPriceChangePercentage;
        
        public string LatestPricesApiEndpoint;
        public string _5MinPricesApiEndpoint;
        public string _1HourPricesApiEndpoint;
        public string _6HourPricesApiEndpoint;
        public string _24HourPricesApiEndpoint;
        public string TimeSeriesApiEndpoint;
        public string MappingApiEndpoint;
        public string IconsApiEndpoint;
        public string WikiApiEndpoint;
        public string PriceInfoPageApiEndpoint;
        public string GeTrackerPageApiEndpoint;

        public uint[] BlacklistedItemIds;

        public Dictionary<string, string> GuildChannelDict;
    }
}