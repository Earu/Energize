using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Energize.Essentials
{
    public enum ThumbnailType
    {
        None,
        Error,
        Warning,
        Music,
        Radio,
        Update,
        NoResult,
    }

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

        private bool TryGetThumbnail(ThumbnailType thumbnailType, out string fileName, out string filePath)
        {
            switch (thumbnailType)
            {
                /*case ThumbnailType.Error:
                    fileName = "error.png";
                    filePath = $"volta/{fileName}";
                    return true;*/
                case ThumbnailType.None:
                default:
                    fileName = null;
                    filePath = null;
                    return false;
            }
        }

        public async Task<IUserMessage> SendAsync(ISocketMessageChannel chan, string header = "", string content = "", EmbedColorType colorType = EmbedColorType.Normal, ThumbnailType thumbnailType = ThumbnailType.None)
        {
            try
            {
                if (!this.CanSendMessage(chan)) return null;
                if (string.IsNullOrWhiteSpace(content)) return null;

                EmbedBuilder builder = new EmbedBuilder();
                builder
                    .WithColorType(colorType)
                    .WithLimitedDescription(content)
                    .WithFooter(header);

                if (this.TryGetThumbnail(thumbnailType, out string fileName, out string filePath))
                {
                    builder.WithThumbnailUrl($"attachment://{fileName}");
                    return await chan.SendFileAsync(filePath, string.Empty, false, builder.Build());
                }

                return await chan.SendMessageAsync(string.Empty, false, builder.Build());
            }
            catch(Exception ex)
            {
                this.LogFailedMessage(chan, ex);
            }

            return null;
        }

        public async Task<IUserMessage> SendAsync(IMessage msg, string header = "", string content = "", EmbedColorType colorType = EmbedColorType.Normal, ThumbnailType thumbnailType = ThumbnailType.None)
            => await this.SendAsync(msg.Channel as ISocketMessageChannel, header, content, colorType, thumbnailType);

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

        public async Task<IUserMessage> SendAsync(IMessage msg, Embed embed = null)
            => await this.SendAsync(msg.Channel, embed);

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

        public async Task<IUserMessage> SendRawAsync(IMessage msg, string content)
            => await this.SendRawAsync(msg.Channel, content);

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

        public async Task<IUserMessage> SendNormalAsync(IMessage msg, string header, string content, ThumbnailType thumbType = ThumbnailType.None)
            => await this.SendAsync(msg, header, content, EmbedColorType.Normal, thumbType);

        public async Task<IUserMessage> SendNormalAsync(IChannel chan, string header, string content, ThumbnailType thumbType = ThumbnailType.None)
            => await this.SendAsync(chan as ISocketMessageChannel, header, content, EmbedColorType.Normal, thumbType);

        public async Task<IUserMessage> SendWarningAsync(IMessage msg, string header, string content, ThumbnailType thumbType = ThumbnailType.None)
            => await this.SendAsync(msg, header, content, EmbedColorType.Warning, thumbType);

        public async Task<IUserMessage> SendWarningAsync(IChannel chan, string header, string content, ThumbnailType thumbType = ThumbnailType.None)
            => await this.SendAsync(chan as ISocketMessageChannel, header, content, EmbedColorType.Warning, thumbType);

        public async Task<IUserMessage> SendDangerAsync(IMessage msg, string header, string content, ThumbnailType thumbType = ThumbnailType.None)
            => await this.SendAsync(msg, header, content, EmbedColorType.Danger, thumbType);

        public async Task<IUserMessage> SendDangerAsync(IChannel chan, string header, string content, ThumbnailType thumbType = ThumbnailType.None)
            => await this.SendAsync(chan as ISocketMessageChannel, header, content, EmbedColorType.Danger, thumbType);

        public async Task<IUserMessage> SendGoodAsync(IMessage msg, string header, string content, ThumbnailType thumbType = ThumbnailType.None)
            => await this.SendAsync(msg, header, content, EmbedColorType.Good, thumbType);

        public async Task<IUserMessage> SendGoodAsync(IChannel chan, string header, string content, ThumbnailType thumbType = ThumbnailType.None)
            => await this.SendAsync(chan as ISocketMessageChannel, header, content, EmbedColorType.Good, thumbType);
    }
}
