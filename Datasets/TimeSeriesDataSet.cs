using Newtonsoft.Json;

namespace FloppaFlipper.Datasets
{
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
}