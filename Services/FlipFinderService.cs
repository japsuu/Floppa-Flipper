using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Discord.WebSocket;
using FloppaFlipper.Datasets;
using FloppaFlipper.Handlers;

namespace FloppaFlipper.Services
{
    public class FlipFinderService
    {
        private readonly DataFetchService dataFetchService;
        private readonly FlipNotifierService flipNotifierService;
        private readonly Timer timer;

        private readonly List<ItemDataSet> crashedItems;
        private readonly List<ItemDataSet> spikedItems;

        public FlipFinderService(LoggingService loggingService, DiscordSocketClient client)
        {
            // Init the services
            dataFetchService = new DataFetchService(loggingService);
            flipNotifierService = new FlipNotifierService(client);
            timer = new Timer();
            crashedItems = new List<ItemDataSet>();
            spikedItems = new List<ItemDataSet>();
            
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
        
        public async void TimerTick(object sender, EventArgs e)
        {
            timer.Enabled = false;

            await dataFetchService.UpdateItemPrices();
            
            Console.Write(":");

            await CalculateCrashedItems();
            await CalculateSpikedItems();
            
            Console.Write(".");

            await flipNotifierService.NotifyFlips(crashedItems, spikedItems);
            
            timer.Enabled = true;
        }

        private async Task CalculateCrashedItems()
        {
            crashedItems.Clear();
            
            foreach (ItemDataSet item in dataFetchService.ItemDataDict.Values)
            {
                // Check that the item is flippable
                if(!item.IsFlippable()) continue;
                
                // Check that the item has dipped
                if(!await item.HasCrashed(ConfigHandler.Config.MinPriceChangePercentage)) continue;

                crashedItems.Add(item);
            }
        }

        private async Task CalculateSpikedItems()
        {
            spikedItems.Clear();
            
            foreach (ItemDataSet item in dataFetchService.ItemDataDict.Values)
            {
                // Check that the item is flippable
                if(!item.IsFlippable()) continue;
                
                // Check that the item has dipped
                if(!await item.HasSpiked(ConfigHandler.Config.MinPriceChangePercentage)) continue;

                spikedItems.Add(item);
            }
        }
    }
}