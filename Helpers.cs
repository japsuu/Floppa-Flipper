using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using FloppaFlipper.Datasets;
using Color = Discord.Color;

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

        public static string GetGraphBackgroundPath()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                @"Media\graph_background.png");
        }
        
        public static byte[] ImageToBytes(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="backgroundPath"></param>
        /// <param name="dataSets">Sample points. 280x = 24h.</param>
        /// <returns></returns>
        public static Bitmap DrawGraph(string backgroundPath, List<TimeSeriesDataSet> dataSets)
        {
            // 460x80
            Image img = Image.FromFile(backgroundPath);
            Bitmap bmp = new Bitmap(img);
            using Graphics graph = Graphics.FromImage(bmp);

            List<PointF> points = new();

            // Get the lowest and highest values in the dataset
            int minValue = int.MaxValue;
            int maxValue = int.MinValue;
            foreach (TimeSeriesDataSet set in dataSets)
            {
                if (set.AvgLowPrice == null) continue;

                if ((int) set.AvgLowPrice < minValue) minValue = (int) set.AvgLowPrice;
                if ((int) set.AvgLowPrice > maxValue) maxValue = (int) set.AvgLowPrice;
            }

            for (int i = 0; i < dataSets.Count; i++)
            {
                if(dataSets[i].AvgLowPrice == null) continue;

                points.Add(NormalizedPointFromValues(i, (int) dataSets[i].AvgLowPrice, img.Width, img.Height, 15, 15,
                    dataSets.Count, minValue, maxValue));
            }

            Pen graphingPen = new Pen(System.Drawing.Color.Chartreuse, 2);
            Pen last6HPen = new Pen(System.Drawing.Color.Brown, 3);
            
            // Draw the graph itself
            graph.DrawLines(graphingPen, points.ToArray());
            
            // Mirror the image
            bmp.RotateFlip(RotateFlipType.Rotate180FlipX);
            
            // Draw the helper lines
            graph.DrawLine(last6HPen, img.Width - (img.Width / 4 - 15), img.Height - 10, img.Width - 15, img.Height - 10);
            
            PrivateFontCollection pfc = new PrivateFontCollection();
            pfc.AddFontFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                @"Fonts\runescape_uf.ttf"));
            graph.DrawString("last 6h ->", new Font(pfc.Families[0], 12, FontStyle.Regular), Brushes.Wheat, new PointF(img.Width - (img.Width / 4 - 15) - 70, img.Height - 18));

            return bmp;
        }

        public static PointF NormalizedPointFromValues(int pointIndex, int itemValue, int imageWidth, int imageHeight, int xPadding, int yPadding, int sampleCount, int lowestValue, int highestValue)
        {
            // Points should be in the range [15-445] on the x-axis, and [15-65] on the y-axis. This gives a range of 430 and 50, respectively.
            // X: oldValue=pointIndex, oldMin=0, oldMax=sampleCount-1, newMin=xPadding, newMax=imageWidth-xPadding
            // Y: oldValue=itemValue, oldMin=lowestValue, oldMax=highestValue, newMin=yPadding, newMax=imageHeight-yPadding
            
            //       NewValue = (((OldValue - OldMin) * (NewMax - NewMin)) / (OldMax - OldMin)) + NewMin
            float normalizedX = ((pointIndex * (imageWidth - xPadding - xPadding)) / (sampleCount - 1)) + xPadding;
            float normalizedY = (((itemValue - lowestValue) * (imageHeight - yPadding - yPadding)) / (highestValue - lowestValue)) + yPadding;

            return new PointF(normalizedX, normalizedY);
        }
    }
}