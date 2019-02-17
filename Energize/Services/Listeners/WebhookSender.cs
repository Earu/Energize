using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using Energize.ServiceInterfaces;
using Energize.Toolkit;
using System;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    [Service("Webhook")]
    class WebhookSender : IServiceImplementation
    {
        private static ulong _ID = 0;

        private readonly EnergizeClient _EClient;
        private readonly Logger _Log;

        public WebhookSender(EnergizeClient eclient)
        {
            this._EClient = eclient;
            this._Log = eclient.Logger;
        }

        private async Task<DiscordWebhookClient> CreateWebhook(ITextChannel chan)
        {
            try
            {
                SocketSelfUser bot = this._EClient.DiscordClient.CurrentUser;
                IWebhook webhook = await chan.CreateWebhookAsync(bot.Username + _ID);
                _ID++;

                return new DiscordWebhookClient(webhook);
            }
            catch
            {
                return null;
            }
        }

        private async Task DeleteWebhook(DiscordWebhookClient webhook)
        {
            try
            {
                await webhook.DeleteWebhookAsync();
            }
            catch(Exception e)
            {
                this._Log.Nice("Webhook", ConsoleColor.Red, $"Couldn't get rid of a webhook: {e.Message}");
            }
        }

        private void LogFailedMessage(SocketMessage msg)
        {
            string log = string.Empty;
            if (!(msg.Channel is IDMChannel))
            {
                IGuildChannel chan = msg.Channel as IGuildChannel;
                log += $"({chan.Guild.Name} - #{chan.Name}) {msg.Author.Username} doesn't have <send message> right";
            }
            else
            {
                log += $"(DM) {msg.Author.Username} blocked a message";
            }
            this._Log.Nice("Webhook", ConsoleColor.Red, log);
        }

        private void LogFailedMessage(ITextChannel chan)
        {
            string log = string.Empty;
            if (!(chan is IDMChannel))
                log += $"({chan.Guild.Name} - #{chan.Name}) doesn't have <send message> right";
            else
                log += $"(DM) {chan.Name} blocked a message";
            this._Log.Nice("Webhook", ConsoleColor.Red, log);
        }

        public async Task<ulong> SendRaw(SocketMessage msg,string content,string username,string avatarurl)
        {
            if(msg.Channel is IGuildChannel)
            {
                ITextChannel chan = msg.Channel as ITextChannel;
                DiscordWebhookClient webhook = await this.CreateWebhook(chan);
                ulong id = 0;
                if(webhook != null)
                {
                    try
                    {
                        id = await webhook.SendMessageAsync(content, false, null, username, avatarurl);
                    }
                    catch
                    {
                        this.LogFailedMessage(msg);
                    }
                }

                await this.DeleteWebhook(webhook);
                return id;
            }
            return 0;
        }

        public async Task<ulong> SendRaw(ITextChannel chan, string content, string username, string avatarurl)
        {
            DiscordWebhookClient webhook = await this.CreateWebhook(chan);
            ulong id = 0;
            if (webhook != null)
            {
                try
                {
                    id = await webhook.SendMessageAsync(content, false, null, username, avatarurl);
                }
                catch
                {
                    this.LogFailedMessage(chan);
                }

                await this.DeleteWebhook(webhook);
                return id;
            }

            return id;
        }

        public async Task<ulong> SendEmbed(SocketMessage msg, Embed embed, string username, string avatarurl)
        {
            if (msg.Channel is IGuildChannel)
            {
                ITextChannel chan = msg.Channel as ITextChannel;
                DiscordWebhookClient webhook = await this.CreateWebhook(chan);
                ulong id = 0;
                if (webhook != null)
                {
                    try
                    {
                        id = await webhook.SendMessageAsync("", false, new Embed[] { embed }, username, avatarurl);
                    }
                    catch
                    {
                        this.LogFailedMessage(msg);
                    }

                    await this.DeleteWebhook(webhook);
                }
                return id;
            }

            return 0;
        }

        public async Task<ulong> SendEmbed(ITextChannel chan, Embed embed, string username, string avatarurl)
        {
            DiscordWebhookClient webhook = await this.CreateWebhook(chan);
            ulong id = 0;
            if (webhook != null)
            {
                try
                {
                    id = await webhook.SendMessageAsync("", false, new Embed[] { embed }, username, avatarurl);
                }
                catch
                {
                    this.LogFailedMessage(chan);
                }

                await this.DeleteWebhook(webhook);
            }

            return id;
        }

        public void Initialize() { }

        public Task InitializeAsync()
            => Task.CompletedTask;
    }
}
