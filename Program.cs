using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FloppaFlipper.Modules;

namespace FloppaFlipper
{
    
    /// <summary>
    /// TODO: Change to using a config file like in BoltMailer.
    /// </summary>
    
    internal class Program
    {
        private const string BotToken = "ODk0NDYxMjQwMDYyMTE1ODcw.YVqV8Q.w5nXVT80cQXYkLIqueq57rdFxLg";
        /// <summary>
        /// In seconds.
        /// </summary>
        private const int RefreshRate = 30;

        /// <summary>
        /// In minutes.
        /// </summary>
        public const int ItemNotificationCooldown = 15;
        
        public const int MaxSparklineDatasetLength = 1800;

        public const int MinTradedVolume = 10000;
        
        public const int MinBuyPrice = 200;
        
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();
        
        private DiscordSocketClient socketClient;
        private CommandService commandService;
        private CommandHandler commandHandler;

        private async Task MainAsync()
        {
            var config = new DiscordSocketConfig { MessageCacheSize = 100 };
            socketClient = new DiscordSocketClient(config);
            var cConfig = new CommandServiceConfig { CaseSensitiveCommands = false };
            commandService = new CommandService(cConfig);

            commandHandler = new CommandHandler(socketClient, commandService);

            socketClient.Log += Log;
            socketClient.MessageUpdated += MessageUpdated;
            socketClient.Ready += () =>
            {
                Console.WriteLine("Bot is connected!");
                
                FlipperModule.FetchItemMappings();
                FlipperModule.Timer.Interval = RefreshRate * 1000;
                FlipperModule.Timer.Elapsed += FlipperModule.TimerTick;
                FlipperModule.Timer.Start();
                FlipperModule.TimerTick(null, null);
                
                return Task.CompletedTask;
            };

            await socketClient.LoginAsync(TokenType.Bot, BotToken);
            await socketClient.StartAsync();

            await commandHandler.InstallCommandsAsync();

            await Task.Delay(-1);
        }

        private async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            // If the message was not in the cache, downloading it will result in getting a copy of `after`.
            var message = await before.GetOrDownloadAsync();
            Console.WriteLine($"{message} -> {after}");
        }
        
        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
    
    
    
    /*
    internal static class Program
    {
        private const string PricesApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/latest";
        private const string ItemInfoApiEndpoint = "https://prices.runescape.wiki/api/v1/osrs/mapping";
        private const string BotToken = "ODk0NDYxMjQwMDYyMTE1ODcw.YVqV8Q.w5nXVT80cQXYkLIqueq57rdFxLg";
        
        private static readonly Dictionary<int, ItemInfo> InfoDict = new();
        private static List<ItemInfo> infoList = new();
        
        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<PictureService>()
                .BuildServiceProvider();
        }
        
        public static void Main(string[] args)
            => MainAsync().GetAwaiter().GetResult();

        private static async Task MainAsync()
        {
            await using var services = ConfigureServices();
            
            var client = services.GetRequiredService<DiscordSocketClient>();

            client.Log += LogAsync;
            client.Ready += ReadyAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;

            await client.LoginAsync(TokenType.Bot, BotToken);
            await client.StartAsync();

            // Here we initialize the logic required to register our commands.
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            await Task.Delay(-1);
        }
        
        private static Task ReadyAsync()
        {
            Console.WriteLine("We are connected!");

            return Task.CompletedTask;
        }
        
        private static Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private static void GetItemData()
        {
            Console.WriteLine("Querying item data...");
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ItemInfoApiEndpoint);
            
            request.UserAgent = "FloppaFlipper - Japsu#8887";
            request.Method = "GET";
            
            HttpWebResponse infoResponse = (HttpWebResponse)request.GetResponse();
            
            Console.WriteLine("Connection: " + infoResponse.StatusCode);

            string infoJsonString;
            using (Stream stream = infoResponse.GetResponseStream())
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
        
        private static void GetPrices()
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
                        
                        itemInfo.HighTime = highTime;
                    }

                    if (double.TryParse((string) value["lowTime"] ?? string.Empty, out double unixL))
                    {
                        DateTime lowTime = UnixTimeStampToDateTime(unixL);
                        
                        itemInfo.LowTime = lowTime;
                    }
                    
                    string high = (string)value["high"];
                    itemInfo.High = high;
                    
                    string low = (string)value["low"];
                    itemInfo.Low = low;
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
    }*/
}