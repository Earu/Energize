using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.Services.Senders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Energize.Services.Senders
{
    [Service("Webhook")]
    public class WebhookSenderService : ServiceImplementationBase, IWebhookSenderService
    {
        private readonly Logger Logger;

        public WebhookSenderService(EnergizeClient client)
        {
            this.Logger = client.Logger;
        }

        private static bool CanUseWebhooks(IChannel chan)
        {
            if (!(chan is SocketGuildChannel guildChannel))
                return false;

            SocketGuildUser botUser = guildChannel.Guild.CurrentUser;
            return botUser.GetPermissions(guildChannel).ManageWebhooks;
        }

        private async Task<DiscordWebhookClient> CreateWebhook(string name, ITextChannel chan)
        {
            try
            {
                if (!CanUseWebhooks(chan)) return null;

                IWebhook webhook = await chan.CreateWebhookAsync(name);
                return new DiscordWebhookClient(webhook);
            }
            catch(Exception ex)
            {
                this.Logger.Nice("Webhook", ConsoleColor.Red, $"Could not create webhook: {ex.Message}");
                return null;
            }
        }

        private async Task<DiscordWebhookClient> GetOrCreateWebhook(string name, ITextChannel chan)
        {
            try
            {
                if (!CanUseWebhooks(chan)) return null;

                IReadOnlyCollection<IWebhook> webhooks = await chan.GetWebhooksAsync();
                IWebhook webhook = webhooks.FirstOrDefault(x => x.Name == name);
                return webhook == null ? await this.CreateWebhook(name, chan) : new DiscordWebhookClient(webhook);
            }
            catch (Exception ex)
            {
                this.Logger.Nice("Webhook", ConsoleColor.Red, $"Could not get a list of webhooks: {ex.Message}");
                return null;
            }  
        }

        private async Task<DiscordWebhookClient> TryGetWebhook(IChannel chan)
        {
            if (chan is IDMChannel) return null;

            SocketGuildChannel guildChan = (SocketGuildChannel)chan;
            SocketGuildUser botUser = guildChan.Guild.CurrentUser;
            if (!botUser.GetPermissions(guildChan).ManageWebhooks)
                return null;

            return await this.GetOrCreateWebhook(botUser.Username, (ITextChannel)guildChan);
        }

        public async Task<ulong> SendRaw(IMessage msg, string content, string username, string avatarUrl)
        {
            DiscordWebhookClient webhook = await this.TryGetWebhook(msg.Channel);
            if (webhook == null) return 0;

            try
            {
                return await webhook.SendMessageAsync(content, false, null, username, avatarUrl);
            }
            catch (Exception ex)
            {
                this.Logger.Nice("Webhook", ConsoleColor.Red, $"Could not send a message: {ex.Message}");

                return 0;
            }
        }

        public async Task<ulong> SendRaw(ITextChannel chan, string content, string username, string avatarUrl)
        {
            DiscordWebhookClient webhook = await this.TryGetWebhook(chan);
            if (webhook == null) return 0;

            try
            {
                return await webhook.SendMessageAsync(content, false, null, username, avatarUrl);
            }
            catch (Exception ex)
            {
                this.Logger.Nice("Webhook", ConsoleColor.Red, $"Could not send a message: {ex.Message}");
                return 0;
            }
        }

        public async Task<ulong> SendEmbed(IMessage msg, Embed embed, string username, string avatarUrl)
        {
            DiscordWebhookClient webhook = await this.TryGetWebhook(msg.Channel);
            if (webhook == null) return 0;

            try
            {
                return await webhook.SendMessageAsync(string.Empty, false, new [] { embed }, username, avatarUrl);
            }
            catch (Exception ex)
            {
                this.Logger.Nice("Webhook", ConsoleColor.Red, $"Could not send a message: {ex.Message}");
                return 0;
            }
        }

        public async Task<ulong> SendEmbed(ITextChannel chan, Embed embed, string username, string avatarUrl)
        {
            DiscordWebhookClient webhook = await this.TryGetWebhook(chan);
            if (webhook == null) return 0;

            try
            {
                return await webhook.SendMessageAsync(string.Empty, false, new [] { embed }, username, avatarUrl);
            }
            catch (Exception ex)
            {
                this.Logger.Nice("Webhook", ConsoleColor.Red, $"Could not send a message: {ex.Message}");
                return 0;
            }
        }
    }
}
