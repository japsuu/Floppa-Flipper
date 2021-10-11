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
        
        private async void TimerTick(object sender, EventArgs e)
        {
            timer.Enabled = false;

            await dataFetchService.UpdateItemPrices();

            CalculateCrashedItems();
            CalculateSpikedItems();

            await flipNotifierService.NotifyFlips(crashedItems, spikedItems);
            
            timer.Enabled = true;
        }

        private void CalculateCrashedItems()
        {
            crashedItems.Clear();
            
            foreach (ItemDataSet item in dataFetchService.ItemDataDict.Values)
            {
                // Check that the item is flippable
                if(!item.IsFlippable()) continue;
                
                // Check that the item has dipped
                if(!item.HasCrashed(5.00)) continue;

                crashedItems.Add(item);
            }
        }

        private void CalculateSpikedItems()
        {
            spikedItems.Clear();
            
            foreach (ItemDataSet item in dataFetchService.ItemDataDict.Values)
            {
                // Check that the item is flippable
                if(!item.IsFlippable()) continue;
                
                // Check that the item has dipped
                if(!item.HasSpiked(5.00)) continue;

                spikedItems.Add(item);
            }
        }
    }
}