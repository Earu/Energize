using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.Services.Senders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Energize.Services.Senders
{
    [Service("Webhook")]
    public class WebhookSenderService : ServiceImplementationBase, IWebhookSenderService
    {
        private readonly Logger Logger;
        private readonly HttpClient HttpClient;

        public WebhookSenderService(EnergizeClient client)
        {
            this.Logger = client.Logger;
            this.HttpClient = new HttpClient();
        }

        private static bool CanUseWebhooks(IChannel chan)
        {
            if (!(chan is SocketGuildChannel guildChannel))
                return false;

            SocketGuildUser botUser = guildChannel.Guild.CurrentUser;
            return botUser.GetPermissions(guildChannel).ManageWebhooks;
        }

        private async Task<DiscordWebhookClient> CreateWebhookAsync(string name, ITextChannel chan)
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

        private async Task<DiscordWebhookClient> GetOrCreateWebhookAsync(string name, ITextChannel chan)
        {
            try
            {
                if (!CanUseWebhooks(chan)) return null;

                IReadOnlyCollection<IWebhook> webhooks = await chan.GetWebhooksAsync();
                IWebhook webhook = webhooks.FirstOrDefault(x => x.Name == name);
                return webhook == null ? await this.CreateWebhookAsync(name, chan) : new DiscordWebhookClient(webhook);
            }
            catch (Exception ex)
            {
                this.Logger.Nice("Webhook", ConsoleColor.Red, $"Could not get a list of webhooks: {ex.Message}");
                return null;
            }  
        }

        private async Task<DiscordWebhookClient> TryGetWebhookAsync(IChannel chan)
        {
            if (chan is IDMChannel) return null;

            SocketGuildChannel guildChan = (SocketGuildChannel)chan;
            SocketGuildUser botUser = guildChan.Guild.CurrentUser;
            if (!botUser.GetPermissions(guildChan).ManageWebhooks)
                return null;

            return await this.GetOrCreateWebhookAsync(botUser.Username, (ITextChannel)guildChan);
        }

        public async Task<ulong> SendRawAsync(IMessage msg, string content, string username, string avatarUrl)
        {
            using (DiscordWebhookClient webhook = await this.TryGetWebhookAsync(msg.Channel))
            {
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
        }

        public async Task<ulong> SendRawAsync(ITextChannel chan, string content, string username, string avatarUrl)
        {
            using (DiscordWebhookClient webhook = await this.TryGetWebhookAsync(chan))
            {
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
        }

        public async Task<ulong> SendEmbedAsync(IMessage msg, Embed embed, string username, string avatarUrl)
        {
            using (DiscordWebhookClient webhook = await this.TryGetWebhookAsync(msg.Channel))
            {
                if (webhook == null) return 0;

                try
                {
                    return await webhook.SendMessageAsync(string.Empty, false, new[] { embed }, username, avatarUrl);
                }
                catch (Exception ex)
                {
                    this.Logger.Nice("Webhook", ConsoleColor.Red, $"Could not send a message: {ex.Message}");
                    return 0;
                }
            }

        }

        public async Task<ulong> SendEmbedAsync(ITextChannel chan, Embed embed, string username, string avatarUrl)
        {
            using (DiscordWebhookClient webhook = await this.TryGetWebhookAsync(chan))
            {
                if (webhook == null) return 0;

                try
                {
                    return await webhook.SendMessageAsync(string.Empty, false, new[] { embed }, username, avatarUrl);
                }
                catch (Exception ex)
                {
                    this.Logger.Nice("Webhook", ConsoleColor.Red, $"Could not send a message: {ex.Message}");
                    return 0;
                }
            }
        }

        public async Task<ulong> RepostMessageAsync(ITextChannel chan, IMessage msg, Embed embed = null)
        {
            using (DiscordWebhookClient webhook = await this.TryGetWebhookAsync(chan))
            {
                if (webhook == null) return 0;

                try
                {
                    Embed[] embeds = embed == null ? null : new[] { embed };
                    if (msg.Attachments.Count > 0)
                    {
                        IAttachment attachment = msg.Attachments.First();
                        using (Stream stream = await this.HttpClient.GetStreamAsync(attachment.ProxyUrl))
                            return await webhook.SendFileAsync(stream, attachment.Filename, msg.Content, false, embeds, msg.Author.Username, msg.Author.GetAvatarUrl());
                    }
                    else
                    {
                        return await webhook.SendMessageAsync(msg.Content, false, embeds, msg.Author.Username, msg.Author.GetAvatarUrl());
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.Nice("Webhook", ConsoleColor.Red, $"Could not send a message: {ex.Message}");
                    return 0;
                }
            }
        }
    }
}
