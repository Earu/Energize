using Discord;
using Discord.WebSocket;
using Energize.Interfaces.DatabaseModels;
using Energize.Interfaces.Services;
using Energize.Services.Database;
using Energize.Toolkit;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    [Service("Administration")]
    class Administration : IAdministrationService
    {
        private readonly DiscordShardedClient _Client;
        private readonly MessageSender _MessageSender;
        private readonly ServiceManager _ServiceManager;
        private readonly Logger _Logger;

        public Administration(EnergizeClient client)
        {
            this._Client = client.DiscordClient;
            this._MessageSender = client.MessageSender;
            this._ServiceManager = client.ServiceManager;
            this._Logger = client.Logger;
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (!(msg.Channel is IGuildChannel)) return;

            SocketGuildChannel chan = msg.Channel as SocketGuildChannel;
            string pattern = @"discord\.gg\/.+\s?";
            if (Regex.IsMatch(msg.Content, pattern) && msg.Author.Id != Config.BOT_ID_MAIN)
            {
                var db = this._ServiceManager.GetService<DBContextPool>("Database");
                using (IDatabaseContext ctx = await db.GetContext())
                {
                    IDiscordGuild dbguild = await ctx.Instance.GetOrCreateGuild(chan.Guild.Id);
                    if (!dbguild.ShouldDeleteInvites) return;

                    try
                    {
                        EmbedBuilder builder = new EmbedBuilder();
                        this._MessageSender.BuilderWithAuthor(msg, builder);
                        builder.WithDescription("Your message was removed, it contained an invitation link");
                        builder.WithFooter("Invite Checker");
                        builder.WithColor(_MessageSender.ColorWarning);

                        await msg.DeleteAsync();
                        await this._MessageSender.Send(msg, builder.Build());
                    }
                    catch
                    {
                        await this._MessageSender.Danger(msg, "Invite Checker", "I couldn't delete this message"
                            + " because I don't have the rights necessary for that");
                    }
                }
            }
        }

        public async Task InitializeAsync()
        {
            if (!File.Exists("restartlog.txt")) return;

            string content = File.ReadAllText("restartlog.txt");
            if(ulong.TryParse(content, out ulong id))
            {
                SocketChannel chan = this._Client.GetChannel(id);
                if(chan != null)
                    await this._MessageSender.Good(chan, "Restart", "Done restarting.");
            }

            File.Delete("restartlog.txt");
        }

        public void Initialize() { }
    }
}
