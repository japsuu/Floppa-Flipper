using System;

namespace FloppaFlipper.Datasets
{
    public class PriceNotificationDataSet
    {
        public ItemDataSet ItemToTrack;
        public long PriceToNotifyAt;
        public DateTime NotificationCreated;
    }
}