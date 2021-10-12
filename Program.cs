using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FloppaFlipper.Handlers;
using FloppaFlipper.Services;

namespace FloppaFlipper
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            // Read the config on startup
            if (ConfigHandler.Init())
            {
                MainAsync().GetAwaiter().GetResult();
            }
            else
            {
                Console.WriteLine("Config could not be read successfully. Aborting startup.");
                Console.WriteLine("Press any key to exit.");

                Console.ReadKey();
            }
        }

        private static DiscordSocketClient socketClient;
        private static CommandService commandService;
        private static CommandHandler commandHandler;
        private static LoggingService loggingService;
        private static FlipFinderService flipFinderService;

        public static void ForceFlips()
        {
            flipFinderService.TimerTick(null, null);
        }

        private static async Task MainAsync()
        {
            // Create the Discord socket client
            DiscordSocketConfig config = new DiscordSocketConfig { MessageCacheSize = 100 };
            socketClient = new DiscordSocketClient(config);
            
            // Create the command service
            CommandServiceConfig cConfig = new CommandServiceConfig { CaseSensitiveCommands = false };
            commandService = new CommandService(cConfig);
            
            // Create the logging service
            loggingService = new LoggingService(socketClient, commandService);
            
            // Create the data fetch service
            flipFinderService = new FlipFinderService(loggingService, socketClient);

            // Create the command handler
            commandHandler = new CommandHandler(socketClient, commandService);

            // Subscribe to events
            socketClient.MessageUpdated += OnMessageUpdated;
            socketClient.InteractionCreated += OnClientInteractionCreated;
            socketClient.Ready += () =>
            {
                Console.WriteLine("[INFO]: Socket client is ready.");
                
                socketClient.SetActivityAsync(new Game(" the GE-prices.", ActivityType.Watching, ActivityProperties.None,
                    "Try !help for a list of commands."));
                
                return Task.CompletedTask;
            };

            // Login and start the bot
            await socketClient.LoginAsync(TokenType.Bot, ConfigHandler.Config.BotToken);
            await socketClient.StartAsync();

            // Start the flip finder
            await flipFinderService.Start();

            // Initialize the commands
            await commandHandler.InstallCommandsAsync();

            // Never exit this async context
            await Task.Delay(-1);
        }

        private static async Task OnMessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            // If the message was not in the cache, downloading it will result in getting a copy of `after`.
            var message = await before.GetOrDownloadAsync();
            Console.WriteLine($"{message} -> {after}");
        }

        private static async Task OnClientInteractionCreated(SocketInteraction interaction)
        {
            // Checking the type of this interaction
            switch (interaction)
            {
                // Slash commands
                case SocketSlashCommand commandInteraction:
                    await SlashCommandHandler(commandInteraction);
                    break;
      
                // Button clicks/selection dropdowns
                case SocketMessageComponent componentInteraction:
                    await MessageComponentHandler(componentInteraction);
                    break;
      
                // Unused or Unknown/Unsupported
                default:
                    break;
            }
        }
        
        private static async Task SlashCommandHandler(SocketSlashCommand interaction)
        {
            // Checking command name
            if (interaction.Data.Name == "ping")
            {
                // Respond to interaction with message.
                // You can also use "ephemeral" so that only the original user of the interaction sees the message
                await interaction.RespondAsync($"Pong!", ephemeral: true);
      
                // Also you can followup with a additional messages, which also can be "ephemeral"
                await interaction.FollowupAsync($"PongPong!", ephemeral: true);
            }
        }
        
        private static async Task MessageComponentHandler(SocketMessageComponent interaction)
        {
            // Get the custom ID 
            string customId = interaction.Data.CustomId;
            
            // Get the user
            SocketGuildUser user = (SocketGuildUser) interaction.User;
            
            string selectedValue = interaction.Data.Values.First();

            switch (customId)
            {
                case "NotifyMeSelectMenu":
                {
                    await user.SendMessageAsync("I'll make sure to give you a heads up when the price spikes again!");
                    
                    //FlipperModule.OngoingPriceNotifications.Add(user, );
                    
                    break;
                }
                default:
                    break;
            }
    
            // Respond with the update message. This edits the message which this component resides.
            //await interaction.UpdateAsync(msgProps => msgProps.Content = $"Clicked {interaction.Data.CustomId}!");
    
            // Also you can followup with a additional messages
            //await interaction.FollowupAsync($"Clicked {interaction.Data.CustomId}!", ephemeral: true);
    
            // If you are using selection dropdowns, you can get the selected label and values using these
            //var selectedLabel = ((SelectMenu) interaction.Message.Components.First().Components.First()).Options.FirstOrDefault(x => x.Value == interaction.Data.Values.FirstOrDefault())?.Label;
        }
    }
}