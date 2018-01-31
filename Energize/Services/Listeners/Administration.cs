using Discord;
using Discord.WebSocket;
using Energize.Services.Commands;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    [Service("Administration")]
    class Administration
    {
        private EnergizeClient _EClient;

        public Administration(EnergizeClient eclient)
        {
            this._EClient = eclient;
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (msg.Channel is IGuildChannel)
            {
                SocketGuildChannel chan = msg.Channel as SocketGuildChannel;
                string pattern = @"discord\.gg\/.+\s?";
                if (Regex.IsMatch(msg.Content, pattern) && msg.Author.Id != EnergizeConfig.BOT_ID_MAIN)
                {
                    if (chan.Guild.Roles.Any(x => x.Name == "EnergizeDeleteInvites"))
                    {
                        CommandHandler chandler = ServiceManager.GetService<CommandHandler>("Commands");
                        try
                        {
                            EmbedBuilder builder = new EmbedBuilder();
                            chandler.MessageSender.BuilderWithAuthor(msg, builder);
                            builder.WithDescription("Your message was removed, it contained an invitation link");
                            builder.WithFooter("Invite Checker");
                            builder.WithColor(chandler.MessageSender.ColorWarning);

                            await msg.DeleteAsync();
                            await chandler.MessageSender.Send(msg, builder.Build());
                        }
                        catch
                        {
                            await chandler.MessageSender.Danger(msg, "Invite Checker", "I couldn't delete this message"
                                + " because I don't have the rights necessary for that");
                        }

                    }
                }
            }
        }

        public async Task InitializeAsync()
        {
            if (File.Exists("restartlog.txt"))
            {
                string content = File.ReadAllText("restartlog.txt");
                ulong id = ulong.Parse(content.Trim());

                await this._EClient.MessageSender.Good(this._EClient.Discord.GetChannel(id), "Restart", "Done restarting.");
                File.Delete("restartlog.txt");
            }
        }
    }
}
