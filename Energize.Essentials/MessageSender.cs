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
        public MessageSender(Logger log)
            => this.Log = log;

        public static Color SColorGood { get; } = new Color(30, 30, 30);
        public static Color SColorNormal { get; } = new Color(200, 200, 200);
        public static Color SColorWarning { get; } = new Color(226, 123, 68);
        public static Color SColorDanger { get; } = new Color(226, 68, 68);

        public Logger Log { get; private set; }
        public Color ColorGood { get => SColorGood; }
        public Color ColorNormal { get => SColorNormal; }
        public Color ColorWarning { get => SColorWarning; }
        public Color ColorDanger { get => SColorDanger; }

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
            this.Log.Nice("Message", ConsoleColor.Red, log);
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
            this.Log.Nice("Message", ConsoleColor.Red, log);
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

        public async Task<IUserMessage> Send(IMessage msg, string header = "", string content = "", Color color = new Color(), string picurl = null)
        {
            try
            {
                string username = msg.Author.Username;
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(color);
                builder.WithDescription(content);
                builder.WithFooter(header);
                builder.WithAuthorNickname(msg);

                if (picurl != null)
                    builder.WithThumbnailUrl(picurl);

                if (!string.IsNullOrWhiteSpace(content))
                    return await msg.Channel.SendMessageAsync(string.Empty, false, builder.Build());
            }
            catch
            {
                this.LogFailedMessage(msg);
            }

            return null;
        }

        public async Task<IUserMessage> Send(ISocketMessageChannel chan, string header = "", string content = "", Color color = new Color())
        {
            try
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(color);
                builder.WithDescription(content);
                builder.WithFooter(header);

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
                builder.WithColor(this.ColorGood);
                builder.WithDescription(content);
                builder.WithFooter(header);
                builder.WithAuthor(msg.Author);

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
            => await this.Send(msg, header, content, this.ColorNormal);

        public async Task<IUserMessage> Normal(IChannel chan, string header, string content)
            => await this.Send((chan as ISocketMessageChannel), header, content, this.ColorNormal);

        public async Task<IUserMessage> Warning(IMessage msg, string header, string content)
            => await this.Send(msg, header, content, this.ColorWarning);

        public async Task<IUserMessage> Warning(IChannel chan, string header, string content)
            => await this.Send((chan as ISocketMessageChannel), header, content, this.ColorWarning);

        public async Task<IUserMessage> Danger(IMessage msg, string header, string content)
            => await this.Send(msg, header, content, this.ColorDanger);

        public async Task<IUserMessage> Danger(IChannel chan, string header, string content)
            => await this.Send((chan as ISocketMessageChannel), header, content, this.ColorDanger);

        public async Task<IUserMessage> Good(IMessage msg, string header, string content)
            => await this.Send(msg, header, content, this.ColorGood);

        public async Task<IUserMessage> Good(IChannel chan, string header, string content)
            => await this.Send((chan as ISocketMessageChannel), header, content, this.ColorGood);

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
