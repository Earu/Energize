using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Text.RegularExpressions;

namespace Energize.Services.Commands
{
    class Administration
    {
        public static async void OnMessageCreated(SocketMessage msg, CommandReplyEmbed embedrep)
        {
            if (msg.Channel is IDMChannel) return;
            SocketGuildChannel chan = msg.Channel as SocketGuildChannel;
            string pattern = @"https:\/\/discord\.gg\/.+\s?";
            if (Regex.IsMatch(msg.Content, pattern))
            {
                if(chan.Guild.Roles.Any(x => (x.Name == "EnergizeDeleteInvites" || x.Name == "EBotDeleteInvites")))
                {
                    try
                    {
                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithAuthor(msg.Author);
                        builder.WithDescription("Your message was removed, it contained an invitation link");
                        builder.WithFooter("Invite Checker");
                        builder.WithColor(embedrep.ColorWarning);

                        await msg.DeleteAsync();
                        await embedrep.Send(msg, builder.Build());
                    }
                    catch
                    {
                        await embedrep.Danger(msg, "Invite Checker", "I couldn't delete this message"
                            + " because I don't have the rights necessary for that");
                    }

                }
            }
        }
    }
}
