using Discord;
using Discord.Net;
using Discord.Webhook;
using Energize.Interfaces.Services.Senders;
using Energize.Essentials;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private async Task<DiscordWebhookClient> CreateWebhook(ITextChannel chan)
        {
            try
            {
                IUser bot = this._Client.DiscordClient.CurrentUser;
                IWebhook webhook = await chan.CreateWebhookAsync(bot.Username);

                return new DiscordWebhookClient(webhook);
            }
            catch
            {
                return null;
            }
        }

        private async Task<DiscordWebhookClient> GetOrCreateWebhook(ITextChannel chan)
        {
            IUser bot = this._Client.DiscordClient.CurrentUser;
            IReadOnlyCollection<IWebhook> webhooks = new ReadOnlyCollection<IWebhook>(new List<IWebhook>());

            try
            {
                webhooks = await chan.GetWebhooksAsync();
            }
            catch (HttpException)
            {
                this.LogFailedMessage(chan);
                return null;
            }
            catch (Exception e)
            {
                this._Logger.Danger(e);
                return null;
            }

            IWebhook webhook = webhooks.FirstOrDefault(x => x.Name == bot.Username);

            return webhook == null ? await this.CreateWebhook(chan) : new DiscordWebhookClient(webhook);
        }

        private void LogFailedMessage(IMessage msg)
        {
            string log = string.Empty;
            if (!(msg.Channel is IDMChannel))
            {
                IGuildChannel chan = msg.Channel as IGuildChannel;
                log += $"({chan.Guild.Name} - #{chan.Name}) {msg.Author.Username} doesn't have <send message> permission";
            }
            else
            {
                log += $"(DM) {msg.Author.Username} blocked a message";
            }
            this._Logger.Nice("Webhook", ConsoleColor.Red, log);
        }

        private void LogFailedMessage(ITextChannel chan)
        {
            string log = string.Empty;
            if (!(chan is IDMChannel))
                log += $"({chan.Guild.Name} - #{chan.Name}) doesn't have <send message> permission";
            else
                log += $"(DM) {chan.Name} blocked a message";
            this._Logger.Nice("Webhook", ConsoleColor.Red, log);
        }

        public async Task<ulong> SendRaw(IMessage msg, string content, string username, string avatarurl)
        {
            if (!(msg.Channel is IGuildChannel))
                return 0;

            ITextChannel chan = msg.Channel as ITextChannel;
            DiscordWebhookClient webhook = await this.GetOrCreateWebhook(chan);
            ulong id = 0;
            if(webhook != null)
            {
                try
                {
                    id = await webhook.SendMessageAsync(content, false, null, username, avatarurl);
                }
                catch (HttpException)
                {
                    this.LogFailedMessage(msg);
                }
                catch (Exception e)
                {
                    this._Logger.Danger(e);
                }
            }

            return id;
        }

        public async Task<ulong> SendRaw(ITextChannel chan, string content, string username, string avatarurl)
        {
            if (!(chan is IGuildChannel))
                return 0;

            DiscordWebhookClient webhook = await this.GetOrCreateWebhook(chan);
            ulong id = 0;
            if (webhook != null)
            {
                try
                {
                    id = await webhook.SendMessageAsync(content, false, null, username, avatarurl);
                }
                catch (HttpException)
                {
                    this.LogFailedMessage(chan);
                }
                catch (Exception e)
                {
                    this._Logger.Danger(e);
                }
            }

            return id;
        }

        public async Task<ulong> SendEmbed(IMessage msg, Embed embed, string username, string avatarurl)
        {
            if (!(msg.Channel is IGuildChannel))
                return 0;

            ITextChannel chan = msg.Channel as ITextChannel;
            DiscordWebhookClient webhook = await this.GetOrCreateWebhook(chan);
            ulong id = 0;
            if (webhook != null)
            {
                try
                {
                    id = await webhook.SendMessageAsync(string.Empty, false, new Embed[] { embed }, username, avatarurl);
                }
                catch (HttpException)
                {
                    this.LogFailedMessage(msg);
                }
                catch (Exception e)
                {
                    this._Logger.Danger(e);
                }
            }

            return id;
        }

        public async Task<ulong> SendEmbed(ITextChannel chan, Embed embed, string username, string avatarurl)
        {
            DiscordWebhookClient webhook = await this.GetOrCreateWebhook(chan);
            ulong id = 0;
            if (webhook != null)
            {
                try
                {
                    id = await webhook.SendMessageAsync(string.Empty, false, new Embed[] { embed }, username, avatarurl);
                }
                catch (HttpException)
                {
                    this.LogFailedMessage(chan);
                }
                catch (Exception e)
                {
                    this._Logger.Danger(e);
                }
            }

            return id;
        }

        public void Initialize() { }

        public Task InitializeAsync()
            => Task.CompletedTask;
    }
}
