using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using FloppaFlipper.Handlers;

namespace FloppaFlipper.Modules
{
    public class ConfigurationModule : ModuleBase<SocketCommandContext>
    {
        [Command("setup")]
        public async Task Setup(ITextChannel channel)
        {
            await channel.SendMessageAsync("I like to eat cement.");

            ConfigHandler.AddGuildChannelBinding(Context.Guild.Id.ToString(), Context.Channel.Id.ToString());
            
            Console.WriteLine("Added a new binding for a channel.");
        }
    }
}