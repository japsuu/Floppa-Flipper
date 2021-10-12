using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using FloppaFlipper.Datasets;
using FloppaFlipper.Handlers;

namespace FloppaFlipper.Services
{
    public class FlipNotifierService
    {
        private readonly DiscordSocketClient client;
        
        public FlipNotifierService(DiscordSocketClient client)
        {
            this.client = client;
            
            Console.WriteLine("[FLIP NOTIFIER SERVICE STARTED]");
        }

        public async Task NotifyFlips(List<ItemDataSet> priceCrashes, List<ItemDataSet> priceSpikes)
        {
            // For each guild this bot is in
            foreach (SocketGuild guild in client.Guilds)
            {
                // Check if that guild has subscribed to get alerts
                if (!ConfigHandler.Config.GuildChannelDict.TryGetValue(guild.Id.ToString(), out string channel))
                    continue;
                
                // Get the configured channel
                SocketTextChannel socketChannel = guild.GetTextChannel(ulong.Parse(channel));

                foreach (ItemDataSet item in priceCrashes)
                {
                    /*
                    #region Building the sparkline

                        if (dataSets == null)
                        {
                            Console.WriteLine("Could not create a dataset for the graph.");
                            continue;
                        }
    
                        string labelsString = "";
                        string dataString = "";
                        
                        // Only select the last 72 items, to match the 6 hour info
                        foreach (TimeSeriesDataSet dataSet in dataSets.Skip(Math.Max(0, dataSets.Count - 72)))
                        {
                            if (dataSet.AvgHighPrice == null)
                            {
                                labelsString += $"'{Helpers.UnixTimeStampToDateTime(dataSet.Timestamp).ToLongTimeString()}'";
                                dataString += "null";
                            }
                            else
                            {
                                labelsString += $"'{Helpers.UnixTimeStampToDateTime(dataSet.Timestamp).ToLongTimeString()}'";
                                dataString += dataSet.AvgLowPrice;
                            }
                        
                            if (dataString.Length > ConfigHandler.Config.MaxSparklineDatasetLength ||
                                labelsString.Length > ConfigHandler.Config.MaxSparklineDatasetLength)
                                break;
    
                            labelsString += ",";
                            dataString += ",";
                        }
    
                        Chart qc = new()
                        {
                            Width = 300,
                            Height = 250,
                            Config = @"
{
    type: 'line',
  data: {
    labels:[" + labelsString + @"],
    datasets: [{
      label: 'Buy price',
      pointRadius: 1.5,
      borderWidth: 1,
      lineTension: 0.2,
      spanGaps: 'true',
      backgroundColor: 'rgba(255, 0, 0, 0.2)',
      borderColor: 'red',
      data: [" + dataString + @"]
    }]
  }
}"
                        };

                    #endregion
*/                    
                    // Build the embed
                    EmbedBuilder embed = new();
                    ComponentBuilder comp = new();
                    comp.WithSelectMenu(
                        new SelectMenuBuilder
                        {
                            CustomId = "NotifyMeSelectMenu",
                            Disabled = false,
                            Placeholder = "Select when to notify if the item recovers:",
                            MinValues = 1,
                            MaxValues = 1,
                            Options = new List<SelectMenuOptionBuilder>()
                            {
                                new("1h average", "when the item reaches the 1h average price"),
                                new("6h average", "when the item reaches the 6h average price"),
                                new("12h average", "when the item reaches the 12h average price")
                            }
                        }
                    );
                    
                    Console.WriteLine(item._5mAverage.AvgBuyPrice);
                    string buyChange = "dropped";
                    if (double.Parse(item._5mAverage.AvgBuyPrice) > double.Parse(item._6hAverage.AvgBuyPrice)) buyChange = "increased";
                    
                    string sellChange = "dropped";
                    if (double.Parse(item._5mAverage.AvgSellPrice) > double.Parse(item._6hAverage.AvgSellPrice)) sellChange = "increased";
                    
                    
                    embed
                        .WithTitle($"**{item.Name}** | potential dip!")
                    
                        .WithDescription(
                            $"[Wiki]({item.WikiLink}) | [prices.runescape.wiki]({ConfigHandler.Config.PriceInfoPageApiEndpoint + item.Id}) | [GE-tracker]({ConfigHandler.Config.GeTrackerPageApiEndpoint + item.Id})")
                    
                        .WithThumbnailUrl(item.IconLink)
                    
                        .WithColor(Helpers.GetColorByPercent(await item.GetChangePercentage(false)))
                    
                        .WithFooter(footer => footer.Text = "Flip fo no hoe")
                    
                        .WithCurrentTimestamp();
    
                    
                    embed
                        .AddField("Today's **buy** volume: " + item._24hAverage.BuyPriceVolume,
                            "Last hour: " + item._1hAverage.BuyPriceVolume);
    
                    
                    embed
                        .AddField("Today's **sell** volume: " + item._24hAverage.SellPriceVolume,
                            "Last hour: " + item._1hAverage.SellPriceVolume);
                    
    
                    embed
                        .AddField(Emote.Parse("<:Buy:894836544442093568>") + $"**Buy price** `has {buyChange} {await item.GetChangePercentage(true):F2}%`:",
                            $"\tLast 6h's average `{item._6hAverage.AvgBuyPrice}` dropped to `{item._5mAverage.AvgBuyPrice}`");
    
                    
                    embed
                        .AddField(Emote.Parse("<:Sell:894836591590256660>") + $"**Sell price** `has {sellChange} {await item.GetChangePercentage(false):F2}%`:",
                            $"\tLast 6h's average `{item._6hAverage.AvgSellPrice}` dropped to `{item._5mAverage.AvgSellPrice}`");
    
                    //embed
                    //    .WithImageUrl(qc.GetShortUrl());
                    
                    string path = Helpers.GetGraphBackgroundPath();

                    if (item._5mTimeSeries != null)
                    {
                        List<TimeSeriesDataSet> data = item._5mTimeSeries.Skip(Math.Max(0, item._5mTimeSeries.Count - 280)).ToList();

                        Bitmap bmp = Helpers.DrawGraph(path, data);
            
                        await using MemoryStream imgStream = new MemoryStream(Helpers.ImageToBytes(bmp));
            
                        embed.WithImageUrl("attachment://graph.png");
                        await socketChannel.SendFileAsync(imgStream, "graph.png", "", false, embed.Build());

                        await socketChannel.SendMessageAsync("🔔 ding dong bing bong 🔔", component: comp.Build());
                    
                        item.TimeLastNotified = DateTime.Now;
                    }
                    else
                    {
                        await socketChannel.SendMessageAsync(embed: embed.Build());

                        await socketChannel.SendMessageAsync("🔔 ding dong bing bong 🔔", component: comp.Build());
                    }
                }
                
                //await socketChannel.SendMessageAsync("Rawr: " + priceCrashes.Count + " : " + priceSpikes.Count);
            }
        }
    }
}