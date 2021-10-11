using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using FloppaFlipper.Datasets;
using FloppaFlipper.Handlers;
using Newtonsoft.Json;
using QuickChart;

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
                    // Get the 5m time series for this item
                    string timeSeriesJson = await JsonHandler.FetchPriceJson(ConfigHandler.Config.TimeSeriesApiEndpoint, item.Id);
                
                    List<TimeSeriesDataSet> dataSets = JsonConvert.DeserializeObject<List<TimeSeriesDataSet>>(timeSeriesJson);

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
                                dataString += dataSet.AvgHighPrice;
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
    
                    string change = "dropped";
                    
                    if (double.Parse(item.LatestBuy) > double.Parse(item._6hAverage.AvgBuyPrice)) change = "increased";
                    
                    
                    embed
                        .WithTitle($"**{item.Name}** | potential dip!")
                    
                        .WithDescription(
                            $"[Wiki]({item.WikiLink}) | [prices.runescape.wiki]({ConfigHandler.Config.PriceInfoPageApiEndpoint + item.Id}) | [GE-tracker]({ConfigHandler.Config.GeTrackerPageApiEndpoint + item.Id})")
                    
                        .WithThumbnailUrl(item.IconLink)
                    
                        .WithColor(Helpers.GetColorByPercent(item.GetChangePercentage(true)))
                    
                        .WithFooter(footer => footer.Text = "Flip fo no hoe")
                    
                        .WithCurrentTimestamp();
    
                    
                    embed
                        .AddField("Today's **buy** volume: " + item._24hAverage.BuyPriceVolume,
                            "Last hour: " + item._1hAverage.BuyPriceVolume);
    
                    
                    embed
                        .AddField("Today's **sell** volume: " + item._24hAverage.SellPriceVolume,
                            "Last hour: " + item._1hAverage.SellPriceVolume);
                    
    
                    embed
                        .AddField(Emote.Parse("<:Buy:894836544442093568>") + $"**Buy price** `has {change} {item.GetChangePercentage(true):F2}%`:",
                            $"\tLast 6h's average `{item._6hAverage.AvgBuyPrice}` dropped to `{item.LatestBuy}`");
    
                    
                    embed
                        .AddField(Emote.Parse("<:Sell:894836591590256660>") + $"**Sell price** `has {change} {item.GetChangePercentage(false):F2}%`:",
                            $"\tLast 6h's average `{item._6hAverage.AvgSellPrice}` dropped to `{item.LatestSell}`");
    
    
                    embed
                        .WithImageUrl(qc.GetShortUrl());
                    
                    
                    item.TimeLastNotified = DateTime.Now;
    
                    await socketChannel.SendMessageAsync(embed: embed.Build());
                }
                
                await socketChannel.SendMessageAsync("Rawr: " + priceCrashes.Count + " : " + priceSpikes.Count);
            }
        }
    }
}