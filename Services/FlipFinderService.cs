using System;
using System.Threading.Tasks;
using System.Timers;
using Discord.WebSocket;
using FloppaFlipper.Handlers;

namespace FloppaFlipper.Services
{
    public class FlipFinderService
    {
        private readonly DataFetchService dataFetchService;
        private readonly FlipNotifierService flipNotifierService;
        private readonly Timer timer;

        public FlipFinderService(LoggingService loggingService, DiscordSocketClient client)
        {
            // Init the services
            dataFetchService = new DataFetchService(loggingService);
            flipNotifierService = new FlipNotifierService(client);
            timer = new Timer();
            
            Console.WriteLine("[FLIP FINDER SERVICE STARTED]");
        }

        /// <summary>
        /// Starts a timer that invokes the flip finder every tick.
        /// </summary>
        public async Task Start()
        {
            // Fetch all the item mappings
            await dataFetchService.FetchItemMappings();
            
            timer.Interval = ConfigHandler.Config.RefreshRate * 1000;
            timer.Elapsed += TimerTick;
            timer.Start();
        }
        
        private async void TimerTick(object sender, EventArgs e)
        {
            timer.Enabled = false;

            await dataFetchService.UpdateItemPrices();

            await flipNotifierService.NotifyFlips();
            
            timer.Enabled = true;
        }
    }
}