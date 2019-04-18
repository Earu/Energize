using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.DatabaseModels;
using Energize.Interfaces.Services.Database;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    [Service("Administration")]
    class AdministrationService : ServiceImplementationBase
    {
        private readonly DiscordShardedClient _Client;
        private readonly MessageSender _MessageSender;
        private readonly ServiceManager _ServiceManager;
        private readonly Logger _Logger;

        public AdministrationService(EnergizeClient client)
        {
            this._Client = client.DiscordClient;
            this._MessageSender = client.MessageSender;
            this._ServiceManager = client.ServiceManager;
            this._Logger = client.Logger;
        }

        private bool IsInviteMessage(IMessage msg)
        {
            string pattern = @"discord\.gg\/.+\s?";
            if (msg.Channel is IGuildChannel)
                return Regex.IsMatch(msg.Content, pattern) && msg.Author.Id == Config.Instance.Discord.BotID;
            else
                return false;
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (!this.IsInviteMessage(msg)) return;
            IGuildChannel chan = (IGuildChannel)msg.Channel;
            IDatabaseService db = this._ServiceManager.GetService<IDatabaseService>("Database");
            using (IDatabaseContext ctx = await db.GetContext())
            {
                IDiscordGuild dbguild = await ctx.Instance.GetOrCreateGuild(chan.GuildId);
                if (!dbguild.ShouldDeleteInvites) return;

                try
                {
                    await msg.DeleteAsync();
                    await this._MessageSender.Warning(msg, "invite checker", "Your message was removed.");
                }
                catch
                {
                    await this._MessageSender.Warning(msg, "invite checker", "Couldn't delete the invite message. Permissions missing.");
                }
            }
        }
    }
}
