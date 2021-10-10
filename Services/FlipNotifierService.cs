using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
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

        public async Task NotifyFlips()
        {
            foreach (SocketGuild guild in client.Guilds)
            {
                if (ConfigHandler.Config.GuildChannelDict.TryGetValue(guild.Id.ToString(), out string channel))
                {
                    await guild.GetTextChannel(ulong.Parse(channel)).SendMessageAsync("Test asd asd asd");
                }
            }
        }
    }
}