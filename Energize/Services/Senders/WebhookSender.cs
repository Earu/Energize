using Discord;
using Discord.Webhook;
using Energize.Essentials;
using Energize.Interfaces.Services.Senders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Energize.Services.Senders
{
    [Service("Webhook")]
    public class WebhookSender : IWebhookSenderService
    {
        private readonly EnergizeClient _Client;
        private readonly Logger _Logger;

        public WebhookSender(EnergizeClient client)
        {
            this._Client = client;
            this._Logger = client.Logger;
        }

        private async Task<DiscordWebhookClient> CreateWebhook(string name, ITextChannel chan)
        {
            try
            {
                IWebhook webhook = await chan.CreateWebhookAsync(name);
                return new DiscordWebhookClient(webhook);
            }
            catch(Exception e)
            {
                this._Logger.Nice("Webhook", ConsoleColor.Red, $"Could not create webhook\n {e}");
                return null;
            }
        }

        private async Task<DiscordWebhookClient> GetOrCreateWebhook(string name, ITextChannel chan)
        {
            try
            {
                IReadOnlyCollection<IWebhook> webhooks = await chan.GetWebhooksAsync();
                IWebhook webhook = webhooks.FirstOrDefault(x => x.Name == name);
                return webhook == null ? await this.CreateWebhook(name, chan) : new DiscordWebhookClient(webhook);
            }
            catch (Exception e)
            {
                this._Logger.Nice("Webhook", ConsoleColor.Red, $"Could not get a list of webhooks\n {e}");
                return null;
            }  
        }

        private async Task<DiscordWebhookClient> TryGetWebhook(IChannel chan)
        {
            if (chan is IDMChannel) return null;

            ITextChannel textchan = (ITextChannel)chan;
            IGuildUser bot = await textchan.Guild.GetCurrentUserAsync();
            if (!bot.GuildPermissions.ManageWebhooks)
                return null;

            DiscordWebhookClient webhook = await this.GetOrCreateWebhook(bot.Username, textchan);
            return webhook;
        }

        public async Task<ulong> SendRaw(IMessage msg, string content, string username, string avatarurl)
        {
            DiscordWebhookClient webhook = await this.TryGetWebhook(msg.Channel);
            if (webhook == null) return 0;

            try
            {
                return await webhook.SendMessageAsync(content, false, null, username, avatarurl);
            }
            catch (Exception e)
            {
                this._Logger.Nice("Webhook", ConsoleColor.Red, $"Could not send a message\n {e}");
                return 0;
            }
        }

        public async Task<ulong> SendRaw(ITextChannel chan, string content, string username, string avatarurl)
        {
            DiscordWebhookClient webhook = await this.TryGetWebhook(chan);
            if (webhook == null) return 0;

            try
            {
                return await webhook.SendMessageAsync(content, false, null, username, avatarurl);
            }

            catch (Exception e)
            {
                this._Logger.Nice("Webhook", ConsoleColor.Red, $"Could not send a message\n {e}");
                return 0;
            }
        }

        public async Task<ulong> SendEmbed(IMessage msg, Embed embed, string username, string avatarurl)
        {
            DiscordWebhookClient webhook = await this.TryGetWebhook(msg.Channel);
            if (webhook == null) return 0;

            try
            {
                return await webhook.SendMessageAsync(string.Empty, false, new Embed[] { embed }, username, avatarurl);
            }
            catch (Exception e)
            {
                this._Logger.Nice("Webhook", ConsoleColor.Red, $"Could not send a message\n {e}");
                return 0;
            }
        }

        public async Task<ulong> SendEmbed(ITextChannel chan, Embed embed, string username, string avatarurl)
        {
            DiscordWebhookClient webhook = await this.TryGetWebhook(chan);
            if (webhook == null) return 0;

            try
            {
                return await webhook.SendMessageAsync(string.Empty, false, new Embed[] { embed }, username, avatarurl);
            }
            catch (Exception e)
            {
                this._Logger.Nice("Webhook", ConsoleColor.Red, $"Could not send a message\n {e}");
                return 0;
            }
        }

        public void Initialize() { }

        public Task InitializeAsync()
            => Task.CompletedTask;
    }
}
