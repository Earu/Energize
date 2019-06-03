using Discord;
using Discord.Net;
using Discord.WebSocket;
using System;
using System.Net;
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

        public Logger Logger { get; private set; }

        private void LogFailedMessage(IMessage msg)
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
            this.Logger.Nice("Message", ConsoleColor.Red, log);
        }

        private void LogFailedMessage(IChannel chan)
        {
            string log = string.Empty;
            if (!(chan is IDMChannel))
            {
                IGuildChannel c = chan as IGuildChannel;
                log += $"({c.Guild.Name} - #{c.Name}) doesn't have <send message> right";
            }
            else
            {
                IDMChannel c = chan as IDMChannel;
                log += $"(DM) {c.Recipient} blocked a message";
            }
            this.Logger.Nice("Message", ConsoleColor.Red, log);
        }

        public async Task TriggerTyping(ISocketMessageChannel chan)
        {
            try
            {
                await chan.TriggerTypingAsync();
            }
            catch(HttpException ex)
            {
                if (ex.HttpCode != HttpStatusCode.Forbidden)
                    this.LogFailedMessage(chan);
            }
        }

        public async Task<IUserMessage> Send(IMessage msg, string header = "", string content = "", EmbedColorType colorType = EmbedColorType.Normal, string picUrl = null)
        {
            try
            {
                string userName = msg.Author.Username;
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
            catch
            {
                this.LogFailedMessage(msg);
            }

            return null;
        }

        public async Task<IUserMessage> Send(ISocketMessageChannel chan, string header = "", string content = "", EmbedColorType colorType = EmbedColorType.Normal)
        {
            try
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder
                    .WithColorType(colorType)
                    .WithLimitedDescription(content)
                    .WithFooter(header);

                if (!string.IsNullOrWhiteSpace(content))
                    return await chan.SendMessageAsync(string.Empty, false, builder.Build());
            }
            catch
            {
                this.LogFailedMessage(chan as IChannel);
            }

            return null;
        }

        public async Task<IUserMessage> Send(IMessage msg, Embed embed = null)
        {
            try
            {
                return await msg.Channel.SendMessageAsync(string.Empty, false, embed);
            }
            catch
            {
                this.LogFailedMessage(msg);
            }

            return null;
        }

        public async Task<IUserMessage> SendRaw(IMessage msg, string content)
        {
            try
            {
                return await msg.Channel.SendMessageAsync(content);
            }
            catch
            {
                this.LogFailedMessage(msg);
            }

            return null;
        }

        public async Task<IUserMessage> Send(IChannel chan, Embed embed = null)
        {
            try
            {
                IMessageChannel c = chan as IMessageChannel;
                return await c.SendMessageAsync(string.Empty, false, embed);
            }
            catch
            {
                this.LogFailedMessage(chan);
            }

            return null;
        }

        public async Task<IUserMessage> SendRaw(IChannel chan, string content)
        {
            try
            {
                IMessageChannel c = chan as IMessageChannel;
                return await c.SendMessageAsync(content);
            }
            catch
            {
                this.LogFailedMessage(chan);
            }

            return null;
        }

        public async Task<IUserMessage> RespondByDM(IMessage msg, string header = "", string content = "")
        {
            try
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder
                    .WithColorType(EmbedColorType.Good)
                    .WithLimitedDescription(content)
                    .WithFooter(header)
                    .WithAuthor(msg.Author);

                IDMChannel chan = await msg.Author.GetOrCreateDMChannelAsync();
                return await chan.SendMessageAsync(string.Empty, false, builder.Build());
            }
            catch
            {
                this.LogFailedMessage(msg);
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

        public async Task Disconnect(DiscordSocketClient client)
            => await client.LogoutAsync();

        public async Task<IUserMessage> SendFile(IMessage msg, string path)
        {
            try
            {
                return await msg.Channel.SendFileAsync(path);
            }
            catch
            {
                this.LogFailedMessage(msg);
            }

            return null;
        }
    }
}
