using System;
using EBot.Logs;
using EBot.Utils;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;

namespace EBot.Commands.Modules
{
    [CommandModule(Name="Image")]
    class ImageCommands : CommandModule, ICommandModule
    {
        private delegate string SaveCallback(Image<Rgba32> img, string path);

        [Command(Name="avatar",Help="Gets the avatar of a user",Usage="avatar <@user|nothing>")]
        private async Task Avatar(CommandContext ctx)
        {
            SocketUser user = ctx.Message.Author;
            if (ctx.TryGetUser(ctx.Arguments[0],out SocketUser u))
            {
                user = u;
            }

            string url = user.GetAvatarUrl(ImageFormat.Auto,512);
            EmbedBuilder builder = new EmbedBuilder();
            ctx.EmbedReply.BuilderWithAuthor(ctx.Message,builder);
            builder.WithColor(ctx.EmbedReply.ColorGood);
            builder.WithImageUrl(url);
            builder.WithFooter("Avatar");

            await ctx.EmbedReply.Send(ctx.Message, builder.Build());
        }

        [Command(Name="e",Help="Gets the picture of an emote",Usage="e <emote>")]
        private async Task Emote(CommandContext ctx)
        {
            if (!ctx.HasArguments)
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Emote", "You didn't provide any emote");
                return;
            }

            if(Discord.Emote.TryParse(ctx.Arguments[0],out Emote emote))
            {
                EmbedBuilder builder = new EmbedBuilder();
                
                ctx.EmbedReply.BuilderWithAuthor(ctx.Message,builder);
                builder.WithFooter("Emote");
                builder.WithImageUrl(emote.Url);
                builder.WithColor(ctx.EmbedReply.ColorGood);

                await ctx.EmbedReply.Send(ctx.Message, builder.Build());
            }
            /*else if()
            {
                
            }*/
            else if(!string.IsNullOrWhiteSpace(ctx.Arguments[0]))
            {
                string input = ctx.Arguments[0];
                string indicator = ":regional_indicator_";
                string result = "";
                for (int i = 0; i < input.Length; i++)
                {
                    string letter = input[i].ToString().ToLower();
                    if (Regex.IsMatch(letter, @"[A-Za-z]"))
                    {
                        result += indicator + letter + ": ";
                    }
                    else if (Regex.IsMatch(letter, @"\s"))
                    {
                        result += "\t";
                    }
                }

                await ctx.EmbedReply.Good(ctx.Message, "Emote", result);
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Emote", "You didn't provide a valid input");
            }
        }

        private async Task Process(CommandContext ctx,string name,Action<Image<Rgba32>,int> callback=null,SaveCallback savecallback=null)
        {
            string url = null;
            string path = null;
            int value = 0;
            
            if(ctx.TryGetUser(ctx.Arguments[0],out SocketUser user))
            {
                url = user.GetAvatarUrl(ImageFormat.Png, 512);
                if(ctx.Arguments.Count > 1)
                {
                    if(int.TryParse(ctx.Arguments[1],out int arg))
                    {
                        value = arg;
                    }
                }
            }
            else if (Discord.Emote.TryParse(ctx.Arguments[0],out Emote emote))
            {
                url = emote.Url;
                if(ctx.Arguments.Count > 1)
                {
                    if(int.TryParse(ctx.Arguments[1],out int arg))
                    {
                        value = arg;
                    }
                }
            }
            else if (int.TryParse(ctx.Arguments[0],out int arg))
            {
                value = arg;
                url = ctx.LastPictureURL;
            }
            else
            {
                url = ctx.HasArguments ? ctx.Arguments[0] : ctx.LastPictureURL;
                if (ctx.Arguments.Count > 1)
                {
                    if (int.TryParse(ctx.Arguments[1], out int argu))
                    {
                        value = argu;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(url)) //if LastPictureURL is null
            {
                try //if the input is wrong
                {
                    path = await ImageProcess.DownloadImage(url);
                }
                catch
                {
                    path = null;
                }
            }

            if (path != null)
            {
                Image<Rgba32> img = ImageProcess.Get(path);
                value = value > 0 ? value : 1;
                callback?.Invoke(img,value);
                if(savecallback == null)
                {
                    img.Save(path);
                }
                else
                {
                    path = savecallback(img, path);
                }

                await ctx.EmbedReply.SendFile(ctx.Message, path);

                ImageProcess.DeleteImage(path);
            }

            if (path == null)
            {
                await ctx.EmbedReply.Danger(ctx.Message, name, "There's no valid url to use!");
            }
        }

        [Command(Name="bw",Help="Makes a picture black and white",Usage= "bw <imageurl|user|nothing>")]
        private async Task BlackWhite(CommandContext ctx)
        {
            await this. Process(ctx, "BW", (img,value) => img.Mutate(x => x.Grayscale(SixLabors.ImageSharp.Processing.GrayscaleMode.Bt601)));
        }

        [Command(Name="jpg",Help="Makes a picture have bad quality",Usage= "jpg [<amount>]|[<imageurl|user>,<amount>]")]
        private async Task Jpg(CommandContext ctx)
        {
            int val = 1; //default
            await this.Process(ctx, "JPG", (img,value) => {
                val = value == 1 ? 100 : value;
            },
            (img, path) => {
                JpegEncoder encoder = new JpegEncoder
                {
                    Quality = 100 - val,
                    IgnoreMetadata = true,
                    Subsample = JpegSubsample.Ratio420,
                };

                using (FileStream stream =  File.OpenWrite(path)){
                    encoder.Encode(img, stream);
                }
                
                return path;
            });
        }

        [Command(Name="pixelate",Help="pixelate a picture",Usage="pixelate [<amount>]|[<imageurl|user>,<amount>]")]
        private async Task Pixelate(CommandContext ctx)
        {
            await this.Process(ctx, "Pixelate", (img, value) => {
                img.Mutate(x => x.Pixelate(value));
            });
        }

        [Command(Name="invert",Help="Inverts the colors of a picture",Usage= "invert <imageurl|user|nothing>")]
        private async Task Invert(CommandContext ctx)
        {
            await this.Process(ctx, "Invert", (img, value) => img.Mutate(x => x.Invert()));
        }

        [Command(Name="paint",Help="Makes a picture look like a painting",Usage= "paint <imageurl|user|nothing>")]
        private async Task Paint(CommandContext ctx)
        {
            await this.Process(ctx, "Paint", (img, value) => img.Mutate(x => x.OilPaint()));
        }

        [Command(Name="intensify",Help="Intensifies the colors of a picture",Usage="itensify <imageurl|user|nothing>")]
        private async Task Intensify(CommandContext ctx)
        {
            await this.Process(ctx, "Intensify", (img, value) => img.Mutate(x => {
                x.Saturation(100);
                x.Contrast(75);
            }));
        }

        [Command(Name="blur",Help="Blurs a picture",Usage="blur <imageurl|user|nothing>")]
        private async Task Blur(CommandContext ctx)
        {
            await this.Process(ctx, "Blur", (img, value) => img.Mutate(x => x.BoxBlur()));
        }
          
        [Command(Name="greenify",Help="Greenifies a picture",Usage="greenify <imageurl|user")]
        private async Task Greenify(CommandContext ctx)
        {
            await this.Process(ctx, "Greenify", (img, value) => img.Mutate(x => x.Lomograph()));
        }

        [Command(Name="deepfry",Help="Deepfries a picture",Usage="deepfry <imageurl|user|nothing>")]
        private async Task DeepFry(CommandContext ctx)
        {
            await this.Process(ctx,"Deepfry", (img, value) => img.Mutate(x => {
                x.Pixelate(2);
                for (uint i = 0; i < 5; i++)
                {
                    x.Saturation(50);
                    x.Contrast(30);
                    x.GaussianSharpen();
                    x.Quantize(Quantization.Octree);
                }
            }));
        }

        [Command(Name="inspiro",Help="Gets a quote from inspirobot",Usage="inspiro <nothing>")]
        private async Task Inspiro(CommandContext ctx)
        {
            string endpoint = "http://inspirobot.me/api?generate=true";
            string url = await HTTP.Fetch(endpoint,ctx.Log);
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(ctx.EmbedReply.ColorGood);
            builder.WithImageUrl(url);
            builder.WithFooter("InspiroBot");
            ctx.EmbedReply.BuilderWithAuthor(ctx.Message,builder);

            await ctx.EmbedReply.Send(ctx.Message,builder.Build());
        }

        public void Initialize(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand(this.Avatar);
            handler.LoadCommand(this.Emote);
            handler.LoadCommand(this.BlackWhite);
            handler.LoadCommand(this.Jpg);
            handler.LoadCommand(this.Invert);
            handler.LoadCommand(this.Paint);
            handler.LoadCommand(this.Intensify);
            handler.LoadCommand(this.Blur);
            handler.LoadCommand(this.Greenify);
            handler.LoadCommand(this.DeepFry);
            handler.LoadCommand(this.Pixelate);
            handler.LoadCommand(this.Inspiro);

            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}
