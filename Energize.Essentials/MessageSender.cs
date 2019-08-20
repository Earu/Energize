using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Energize.Essentials
{
    public class MessageSender
    {
        private readonly HttpClient HttpClient;
        public MessageSender(Logger logger)
        {
            this.Logger = logger;
            this.HttpClient = new HttpClient();
        }
        public static Color SColorGood { get; } = new Color(30, 30, 30);
        public static Color SColorNormal { get; } = new Color(79, 84, 92);
        public static Color SColorWarning { get; } = new Color(226, 123, 68);
        public static Color SColorDanger { get; } = new Color(226, 68, 68);
        public static Color SColorSpecial { get; } = new Color(165, 28, 21);

        public Logger Logger { get; }

        private void LogFailedMessage(IMessage msg, Exception ex)
        {
            string log = string.Empty;
            if (msg.Channel is IGuildChannel chan)
                log += $"({chan.Guild.Name} - #{chan.Name}) {msg.Author.Username}: {ex.Message}";
            else
                log += $"(DM) {msg.Author.Username}: {ex.Message}";

            this.Logger.Nice("MessageSender", ConsoleColor.Red, log);
        }

        private void LogFailedMessage(IChannel chan, Exception ex)
        {
            string log = string.Empty;
            if (chan is IGuildChannel guildChan)
            {
                log += $"({guildChan.Guild.Name} - #{guildChan.Name}): {ex.Message}";
            }
            else
            {
                IDMChannel dmChan = (IDMChannel)chan;
                log += $"(DM) {dmChan.Recipient}: {ex.Message}";
            }
            this.Logger.Nice("MessageSender", ConsoleColor.Red, log);
        }

        private bool CanSendMessage(IChannel chan)
        {
            if (chan == null) return false;

            if (chan is SocketGuildChannel guildChannel)
            {
                SocketGuildUser botUser = guildChannel.Guild.CurrentUser;
                return botUser.GetPermissions(guildChannel).SendMessages;
            }

            return true;
        }

        private bool CanSendMessage(IMessage msg)
            => this.CanSendMessage(msg.Channel);

        public async Task TriggerTypingAsync(ISocketMessageChannel chan)
        {
            try
            {
                if (!this.CanSendMessage(chan)) return;

                await chan.TriggerTypingAsync();
            }
            catch(Exception ex)
            {
                this.LogFailedMessage(chan, ex);
            }
        }

        public async Task<IUserMessage> SendAsync(IMessage msg, string header = "", string content = "", EmbedColorType colorType = EmbedColorType.Normal, string picUrl = null)
        {
            try
            {
                if (!this.CanSendMessage(msg)) return null;

                EmbedBuilder builder = new EmbedBuilder();
                builder
                    .WithColorType(colorType)
                    .WithLimitedDescription(content)
                    .WithFooter(header)
                    .WithAuthorNickname(msg);

                if (picUrl != null)
                    builder.WithThumbnailUrl(picUrl);

                if (!string.IsNullOrWhiteSpace(content))
                    return await msg.Channel.SendMessageAsync(string.Empty, false, builder.Build());
            }
            catch(Exception ex)
            {
                this.LogFailedMessage(msg, ex);
            }

            return null;
        }

        public async Task<IUserMessage> SendAsync(ISocketMessageChannel chan, string header = "", string content = "", EmbedColorType colorType = EmbedColorType.Normal)
        {
            try
            {
                if (!this.CanSendMessage(chan)) return null;

                EmbedBuilder builder = new EmbedBuilder();
                builder
                    .WithColorType(colorType)
                    .WithLimitedDescription(content)
                    .WithFooter(header);

                if (!string.IsNullOrWhiteSpace(content))
                    return await chan.SendMessageAsync(string.Empty, false, builder.Build());
            }
            catch(Exception ex)
            {
                this.LogFailedMessage(chan, ex);
            }

            return null;
        }

        public async Task<IUserMessage> SendAsync(IMessage msg, Embed embed = null)
        {
            try
            {
                if (!this.CanSendMessage(msg)) return null;

                return await msg.Channel.SendMessageAsync(string.Empty, false, embed);
            }
            catch(Exception ex)
            {
                this.LogFailedMessage(msg, ex);
            }

            return null;
        }

        public async Task<IUserMessage> SendRawAsync(IMessage msg, string content)
        {
            try
            {
                if (!this.CanSendMessage(msg)) return null;

                return await msg.Channel.SendMessageAsync(content);
            }
            catch(Exception ex)
            {
                this.LogFailedMessage(msg, ex);
            }

            return null;
        }

        public async Task<IUserMessage> SendAsync(IChannel chan, Embed embed = null)
        {
            try
            {
                if (!this.CanSendMessage(chan)) return null;

                IMessageChannel c = (IMessageChannel)chan;
                return await c.SendMessageAsync(string.Empty, false, embed);
            }
            catch(Exception ex)
            {
                this.LogFailedMessage(chan, ex);
            }

            return null;
        }

        public async Task<IUserMessage> SendRawAsync(IChannel chan, string content)
        {
            try
            {
                if (!this.CanSendMessage(chan)) return null;

                IMessageChannel c = (IMessageChannel)chan;
                return await c.SendMessageAsync(content);
            }
            catch(Exception ex)
            {
                this.LogFailedMessage(chan, ex);
            }

            return null;
        }

        public async Task<IUserMessage> RepostMessageAsync(IChannel chan, IMessage msg, Embed embed = null)
        {
            try
            {
                if (!this.CanSendMessage(chan)) return null;

                IMessageChannel c = (IMessageChannel)chan;
                if (msg.Attachments.Count > 0)
                {
                    IAttachment attachment = msg.Attachments.First();
                    using (Stream stream = await this.HttpClient.GetStreamAsync(attachment.ProxyUrl))
                        return await c.SendFileAsync(stream, attachment.Filename, msg.Content, embed: embed);
                }
                else
                {
                    return await c.SendMessageAsync(msg.Content, embed: embed);
                }
            }
            catch (Exception ex)
            {
                this.LogFailedMessage(chan, ex);
            }

            return null;
        }

        public async Task<IUserMessage> SendNormalAsync(IMessage msg, string header, string content)
            => await this.SendAsync(msg, header, content);

        public async Task<IUserMessage> SendNormalAsync(IChannel chan, string header, string content)
            => await this.SendAsync(chan as ISocketMessageChannel, header, content);

        public async Task<IUserMessage> SendWarningAsync(IMessage msg, string header, string content)
            => await this.SendAsync(msg, header, content, EmbedColorType.Warning);

        public async Task<IUserMessage> SendWarningAsync(IChannel chan, string header, string content)
            => await this.SendAsync(chan as ISocketMessageChannel, header, content, EmbedColorType.Warning);

        public async Task<IUserMessage> SendDangerAsync(IMessage msg, string header, string content)
            => await this.SendAsync(msg, header, content, EmbedColorType.Danger);

        public async Task<IUserMessage> SendDangerAsync(IChannel chan, string header, string content)
            => await this.SendAsync(chan as ISocketMessageChannel, header, content, EmbedColorType.Danger);

        public async Task<IUserMessage> SendGoodAsync(IMessage msg, string header, string content)
            => await this.SendAsync(msg, header, content, EmbedColorType.Good);

        public async Task<IUserMessage> SendGoodAsync(IChannel chan, string header, string content)
            => await this.SendAsync(chan as ISocketMessageChannel, header, content, EmbedColorType.Good);
    }
}
