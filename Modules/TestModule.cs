using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
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
        
        // !floppa
        [Command("floppa")]
        [Summary("Echoes a floppa image.")]
        public async Task Floppa()
        {
            WebClient client = new WebClient();
            Stream stream = client.OpenRead("https://api.jbh.rocks/image");
            if (stream != null)
            {
                await Context.Channel.SendFileAsync(stream, "floppa.jpg");
            }

            if (stream != null)
            {
                await stream.FlushAsync();
                stream.Close();
            }

            client.Dispose();
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