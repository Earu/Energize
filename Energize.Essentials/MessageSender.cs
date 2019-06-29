using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Energize.Essentials
{
    public class MessageSender
    {
        public MessageSender(Logger logger)
            => this.Logger = logger;

        public static Color SColorGood { get; } = new Color(30, 30, 30);
        public static Color SColorNormal { get; } = new Color(200, 200, 200);
        public static Color SColorWarning { get; } = new Color(226, 123, 68);
        public static Color SColorDanger { get; } = new Color(226, 68, 68);

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
            if (chan is SocketGuildChannel guildChannel)
            {
                SocketGuildUser botUser = guildChannel.Guild.CurrentUser;
                return botUser.GetPermissions(guildChannel).SendMessages;
            }

            return true;
        }

        private bool CanSendMessage(IMessage msg)
            => this.CanSendMessage(msg.Channel);

        public async Task TriggerTyping(ISocketMessageChannel chan)
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

        public async Task<IUserMessage> Send(IMessage msg, string header = "", string content = "", EmbedColorType colorType = EmbedColorType.Normal, string picUrl = null)
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

        public async Task<IUserMessage> Send(ISocketMessageChannel chan, string header = "", string content = "", EmbedColorType colorType = EmbedColorType.Normal)
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

        public async Task<IUserMessage> Send(IMessage msg, Embed embed = null)
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

        public async Task<IUserMessage> SendRaw(IMessage msg, string content)
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

        public async Task<IUserMessage> Send(IChannel chan, Embed embed = null)
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

        public async Task<IUserMessage> SendRaw(IChannel chan, string content)
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

        public async Task<IUserMessage> Normal(IMessage msg, string header, string content)
            => await this.Send(msg, header, content);

        public async Task<IUserMessage> Normal(IChannel chan, string header, string content)
            => await this.Send((chan as ISocketMessageChannel), header, content);

        public async Task<IUserMessage> Warning(IMessage msg, string header, string content)
            => await this.Send(msg, header, content, EmbedColorType.Warning);

        public async Task<IUserMessage> Warning(IChannel chan, string header, string content)
            => await this.Send((chan as ISocketMessageChannel), header, content, EmbedColorType.Warning);

        public async Task<IUserMessage> Danger(IMessage msg, string header, string content)
            => await this.Send(msg, header, content, EmbedColorType.Danger);

        public async Task<IUserMessage> Danger(IChannel chan, string header, string content)
            => await this.Send((chan as ISocketMessageChannel), header, content, EmbedColorType.Danger);

        public async Task<IUserMessage> Good(IMessage msg, string header, string content)
            => await this.Send(msg, header, content, EmbedColorType.Good);

        public async Task<IUserMessage> Good(IChannel chan, string header, string content)
            => await this.Send((chan as ISocketMessageChannel), header, content, EmbedColorType.Good);
    }
}
