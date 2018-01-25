using Discord;
using Discord.WebSocket;
using Energize.Services.Commands;
using Energize.Services.TextProcessing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    [Service("Hentai")]
    public class Hentai
    {
        //Follow service structure
        public Hentai(EnergizeClient eclient) { }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (msg.Channel is IDMChannel || msg.Author.IsBot) return;
            if (msg.Content.ToLower().Contains("hentai"))
            {
                CommandHandler chandler = ServiceManager.GetService("Commands").Instance as CommandHandler;
                TextStyle style = ServiceManager.GetService("TextStyle").Instance as TextStyle;
                IGuildChannel chan = msg.Channel as IGuildChannel;
                SocketGuild guild = chan.Guild as SocketGuild;
                IReadOnlyList<SocketUser> users = guild.Users as IReadOnlyList<SocketUser>;

                Random rand = new Random();
                string quote = EnergizeData.HENTAI_QUOTES[rand.Next(0, EnergizeData.HENTAI_QUOTES.Length - 1)];
                quote = quote.Replace("{NAME}", msg.Author.Username);
                quote = style.GetStyleResult(quote, "anime");

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(new Color(255, 150, 255));
                builder.WithDescription(quote);
                builder.WithFooter("Hentai");

                await chandler.EmbedReply.Send(msg, builder.Build());
            }
        }
    }
}
