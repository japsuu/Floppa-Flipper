using System;
using Discord;

namespace FloppaFlipper
{
    public static class Helpers
    {
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
            return dateTime;
        }
        
        public static Color GetColorByPercent(double percent)
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
    }
}