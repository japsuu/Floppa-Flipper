using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using FloppaFlipper.Datasets;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;

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
        
        // !force
        [Command("force")]
        [Summary("forces a price update.")]
        public Task Force()
        {
            Program.ForceFlips();
            return Task.CompletedTask;
        }

        [Command("graph")]
        public async Task Graph()
        {
            EmbedBuilder embed = new();
            embed.Title = "Graph test";
            embed.Description = "This embed is for testing graphing capabilities.";
            
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, @"Media\graph_background.png");

            Image img = Image.FromFile(path);
            Bitmap bmp = new Bitmap(img);
            using Graphics graph = Graphics.FromImage(bmp);
            graph.DrawString("Concrete", new Font("Tahoma",40), Brushes.Black, new RectangleF(15, 15, img.Width, img.Height));
            
            await using MemoryStream imgStream = new MemoryStream(Helpers.ImageToBytes(bmp));
            
            embed.WithImageUrl("attachment://graph.png");
            await Context.Channel.SendFileAsync(imgStream, "graph.png", "", false, embed.Build());
        }
    }
}