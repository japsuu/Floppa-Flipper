using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace FloppaFlipper.Modules
{
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        // !ping
        [Command("ping")]
        [Summary("Echoes a message.")]
        public Task Ping()
        {
            return ReplyAsync("pong! 👍");
        }
        
        // !test
        [Command("test")]
        [Summary("Test command.")]
        public Task Test()
        {
            Context.User.SendMessageAsync("Test");
            return ReplyAsync(Context.User.Mention + " : " + Context.User.Discriminator + " : " + Context.Client.Latency + " : " + Context.User.CreatedAt);
        }
    }
}