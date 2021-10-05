using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FloppaFlipper.Modules
{
    public class FlipperModule : ModuleBase<SocketCommandContext>
    {
        private const string PricesApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/latest";
        private const string ItemInfoApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/mapping";
        public const string IconsApiEndpoint = "https://secure.runescape.com/m=itemdb_oldschool/obj_big.gif?id=";
        public const string WikiPageApiEndpoint = "https://oldschool.runescape.wiki/w/";
        public const string OfficialPricePageApiEndpoint = "https://prices.runescape.wiki/osrs/item/";
        
        private static readonly Dictionary<int, ItemInfo> InfoDict = new();
        private static List<ItemInfo> infoList = new();

        public static readonly Timer Timer = new();
        
        [Command("embed")]
        public async Task SendRichEmbedAsync()
        {
            var embed = new EmbedBuilder();
            
            //TODO: Also show 1h prices, and 12h prices. Perform the dip check on those.
            
            embed
                .WithTitle("**Item Name** | potential dip!")
                
                .WithDescription(
                    "[Wiki](https://test.com) | [prices.runescape.wiki](https://prices.runescape.wiki/osrs/item/64) | [GE-tracker](https://www.ge-tracker.com/item/64)")
                
                .WithThumbnailUrl(IconsApiEndpoint + 64)
                
                .WithColor(GetColorByPercent(18.12))
                
                .WithFooter(footer => footer.Text = "Flip fo no hoe")
                
                .WithCurrentTimestamp();


            embed
                .AddField(Emote.Parse("<:Buy:894836544442093568>") + "**Buy price** `has changed 18.12%`:", "\tChanged from `123` to `234`");


            embed
                .AddField(Emote.Parse("<:Sell:894836591590256660>") + "**Sell price** `has changed 18.12%`:", "\tChanged from `123` to `234`");

            //Your embed needs to be built before it is able to be sent
            await ReplyAsync(embed: embed.Build());
        }
        
        [Command("show")]
        [Summary("Show the items with changed margins.")]
        public Task ShowChangedPrices(int amount = 0, double minChangePercentage = 10)
        {
            foreach (ItemInfo info in infoList.Where(i => (i.CurrentBuy != i.PreviousBuy) && (Math.Abs(i.GetChangePercentage()) > minChangePercentage)).Take(amount))
            {
                var embed = new EmbedBuilder();

                string change = "Dropped";
                if (double.Parse(info.CurrentBuy) > double.Parse(info.PreviousBuy)) change = "Increased";

                embed.AddField($"{change} from {info.PreviousBuy} to {info.CurrentBuy}",
                        $"[Wiki]({info.WikiPage})")
                    .WithFooter(footer => footer.Text = "Flip fo no hoe")
                    .WithColor(Color.Blue)
                    .WithTitle(info.Name)
                    .WithDescription($"price has changed {info.GetChangePercentage():F2}%")
                    .WithCurrentTimestamp()
                    .WithThumbnailUrl(info.Icon);
                
                ReplyAsync(embed: embed.Build());
            }

            Emoji tUp = new Emoji("👍");

            if (amount < 1)
            {
                ReplyAsync("Invalid count", false, null, null, null, Context.Message.Reference);
                
                tUp = new Emoji("❌");
            }

            
            return Context.Message.AddReactionAsync(tUp);
        }
        
        public static void FetchItemData()
        {
            Console.WriteLine("Querying item data...");
            
            // Connect to the item info API...
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ItemInfoApiEndpoint);
            request.UserAgent = "FloppaFlipper - Japsu#8887";
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            
            Console.WriteLine("Connection: " + response.StatusCode);

            string infoJsonString;
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                infoJsonString = reader.ReadToEnd();
            }
            
            infoList = JsonConvert.DeserializeObject<List<ItemInfo>>(infoJsonString);
            
            if (infoList == null) return;
            
            foreach (ItemInfo info in infoList)
            {
                InfoDict.Add(info.Id, info);
            }
            
            Console.WriteLine("Done!\n");
        }
        
        public static void TimerTick(object sender, EventArgs e)
        {
            FetchItemPrices();
        }
        
        private static void FetchItemPrices()
        {
            Console.WriteLine("Querying prices...");
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(PricesApiEndpoint);

            request.UserAgent = "FloppaFlipper - Japsu#8887";
            request.Method = "GET";

            HttpWebResponse priceResponse = (HttpWebResponse)request.GetResponse();

            Console.WriteLine("Connection: " + priceResponse.StatusCode);

            string priceJsonString;
            using (Stream stream = priceResponse.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                priceJsonString = reader.ReadToEnd();
            }

            priceJsonString = priceJsonString[8..];
            priceJsonString = priceJsonString.Remove(priceJsonString.Length - 1);

            priceJsonString = priceJsonString.Insert(0, "[");
            priceJsonString = priceJsonString.Insert(priceJsonString.Length, "]");
            
            JArray priceObjects = JArray.Parse(priceJsonString); // parse as array  

            foreach(var jToken in priceObjects)
            {
                var root = (JObject) jToken;
                foreach((string itemId, var value) in root)
                {
                    if (!InfoDict.TryGetValue(int.Parse(itemId), out ItemInfo itemInfo)) continue;
                    
                    if(double.TryParse((string) value["highTime"] ?? string.Empty, out double unixH))
                    {
                        DateTime highTime = UnixTimeStampToDateTime(unixH);
                        
                        itemInfo.LatestBuyTime = highTime;
                    }

                    if (double.TryParse((string) value["lowTime"] ?? string.Empty, out double unixL))
                    {
                        DateTime lowTime = UnixTimeStampToDateTime(unixL);
                        
                        itemInfo.LatestSellTime = lowTime;
                    }
                    
                    string high = (string)value["high"];
                    itemInfo.CurrentBuy = high;
                    
                    string low = (string)value["low"];
                    itemInfo.CurrentSell = low;
                }
            }
            
            Console.WriteLine("Done!");
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
            return dateTime;
        }

        private static Color GetColorByPercent(double percent)
        {
            Color color = Color.Default;

            switch (percent)
            {
                case < 5:
                {
                    color = Color.Blue;
                    break;
                }
                
                case < 10:
                {
                    color = Color.LightOrange;
                    break;
                }
                
                case < 20:
                {
                    color = Color.Orange;
                    break;
                }
                
                case > 20:
                {
                    color = Color.Red;
                    break;
                }
            }

            return color;
        }
    }
}