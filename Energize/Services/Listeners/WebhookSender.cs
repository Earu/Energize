using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    [Service("Webhook")]
    class WebhookSender
    {
        private EnergizeClient _EClient;
        private EnergizeLog _Log;

        public WebhookSender(EnergizeClient eclient)
        {
            this._EClient = eclient;
            this._Log = eclient.Log;
        }

        private async Task<bool> CreateWebhook(ITextChannel chan)
        {
            try
            {
                SocketSelfUser bot = this._EClient.Discord.CurrentUser;
                IWebhook webhook = await chan.CreateWebhookAsync(bot.Username);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<DiscordWebhookClient> GetChannelWebhook(ITextChannel chan)
        {
            string name = this._EClient.Discord.CurrentUser.Username;
            IWebhook webhook = (await chan.Guild.GetWebhooksAsync())
                .Where(x => x.ChannelId == chan.Id && x.Name == name)
                .FirstOrDefault();
            if (webhook != null)
            {
                return new DiscordWebhookClient(webhook);
            }
            else
            {
                return null;
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
            {
                log += $"({chan.Guild.Name} - #{chan.Name}) doesn't have <send message> right";
            }
            else
            {
                log += $"(DM) {chan.Name} blocked a message";
            }
            this._Log.Nice("Webhook", ConsoleColor.Red, log);
        }

        private async Task<DiscordWebhookClient> GetOrCreateWebhook(ITextChannel chan)
        {
            DiscordWebhookClient webhook = await this.GetChannelWebhook(chan);
            if (webhook == null)
            {
                if (await this.CreateWebhook(chan))
                {
                    webhook = await this.GetChannelWebhook(chan);
                }
            }

            return webhook;
        }

        public async Task<ulong> SendRaw(SocketMessage msg,string content,string username,string avatarurl)
        {
            if(msg.Channel is IGuildChannel)
            {
                ITextChannel chan = msg.Channel as ITextChannel;
                DiscordWebhookClient webhook = await this.GetOrCreateWebhook(chan);
                if(webhook != null)
                {
                    try
                    {
                        return await webhook.SendMessageAsync(content, false, null, username, avatarurl);
                    }
                    catch
                    {
                        this.LogFailedMessage(msg);
                    }

                    webhook.Dispose();
                }
            }

            return 0;
        }

        public async Task<ulong> SendRaw(ITextChannel chan, string content, string username, string avatarurl)
        {
            DiscordWebhookClient webhook = await this.GetOrCreateWebhook(chan);
            if (webhook != null)
            {
                try
                {
                    return await webhook.SendMessageAsync(content, false, null, username, avatarurl);
                }
                catch
                {
                    this.LogFailedMessage(chan);
                }

                webhook.Dispose();
            }

            return 0;
        }

        public async Task<ulong> SendEmbed(SocketMessage msg, Embed embed, string username, string avatarurl)
        {
            if (msg.Channel is IGuildChannel)
            {
                ITextChannel chan = msg.Channel as ITextChannel;
                DiscordWebhookClient webhook = await this.GetOrCreateWebhook(chan);
                if (webhook != null)
                {
                    try
                    {
                        return await webhook.SendMessageAsync("", false, new Embed[] { embed }, username, avatarurl);
                    }
                    catch
                    {
                        this.LogFailedMessage(msg);
                    }

                    webhook.Dispose();
                }
            }

            return 0;
        }

        public async Task<ulong> SendEmbed(ITextChannel chan, Embed embed, string username, string avatarurl)
        {
            DiscordWebhookClient webhook = await this.GetOrCreateWebhook(chan);
            if (webhook != null)
            {
                try
                {
                    return await webhook.SendMessageAsync("", false, new Embed[] { embed }, username, avatarurl);
                }
                catch
                {
                    this.LogFailedMessage(chan);
                }

                webhook.Dispose();
            }

            return 0;
        }
    }
}
