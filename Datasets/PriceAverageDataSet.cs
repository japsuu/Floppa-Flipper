namespace FloppaFlipper.Datasets
{
    public class PriceAverageDataSet
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
}