using Discord;
using Discord.WebSocket;
using Energize.Services.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    [Service(Name = "Administration")]
    class Administration
    {
        //Follow service structure
        public Administration(EnergizeClient eclient) { }

        public async Task MessageReceived(SocketMessage msg)
        {
            if (msg.Channel is IDMChannel) return;
            SocketGuildChannel chan = msg.Channel as SocketGuildChannel;
            string pattern = @"discord\.gg\/.+\s?";
            if (Regex.IsMatch(msg.Content, pattern) && msg.Author.Id != EnergizeConfig.BOT_ID_MAIN)
            {
                if (chan.Guild.Roles.Any(x => x.Name == "EnergizeDeleteInvites"))
                {
                    CommandHandler chandler = ServiceManager.GetService("Commands").Instance as CommandHandler;
                    try
                    {
                        EmbedBuilder builder = new EmbedBuilder();
                        await chandler.EmbedReply.BuilderWithAuthor(msg, builder);
                        builder.WithDescription("Your message was removed, it contained an invitation link");
                        builder.WithFooter("Invite Checker");
                        builder.WithColor(chandler.EmbedReply.ColorWarning);

                        await msg.DeleteAsync();
                        await chandler.EmbedReply.Send(msg, builder.Build());
                    }
                    catch
                    {
                        await chandler.EmbedReply.Danger(msg, "Invite Checker", "I couldn't delete this message"
                            + " because I don't have the rights necessary for that");
                    }

                }
            }
        }
    }
}
