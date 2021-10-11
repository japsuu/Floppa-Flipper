using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using FloppaFlipper.Handlers;

namespace FloppaFlipper.Modules
{
    public class ConfigurationModule : ModuleBase<SocketCommandContext>
    {
        [Command("subscribe")]
        public async Task Subscribe(ITextChannel channel)
        {
            ConfigHandler.AddGuildChannelBinding(Context.Guild.Id.ToString(), channel.Id.ToString());
            
            await channel.SendMessageAsync("I like to eat cement.");
            Console.WriteLine("Added a new binding for a channel.");
        }
        
        [Command("unsubscribe")]
        public async Task UnSubscribe()
        {
            ConfigHandler.RemoveGuildChannelBinding(Context.Guild.Id.ToString());
            
            await Context.Channel.SendMessageAsync("Alright, let's flop some other time.");
            Console.WriteLine("Removed the binding of a channel.");
        }
    }
}