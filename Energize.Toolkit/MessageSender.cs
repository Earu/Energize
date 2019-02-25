using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Energize.Toolkit
{
    public class MessageSender
    {
        public MessageSender(Logger log)
            => this.Log = log;

        public Logger Log { get; private set; }
        public Color ColorGood { get; } = new Color(30, 30, 30);
        public Color ColorNormal { get; } = new Color(200, 200, 200);
        public Color ColorWarning { get; } = new Color(226, 123, 68);
        public Color ColorDanger { get; } = new Color(226, 68, 68);

        private void LogFailedMessage(SocketMessage msg)
        {
            string log = "";
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

        private void LogFailedMessage(SocketChannel chan)
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

        private void LogFailedMessage(RestChannel chan)
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

        public void BuilderWithAuthor(SocketMessage msg, EmbedBuilder builder)
        {
            if (msg.Channel is SocketGuildChannel)
            {
                SocketGuildUser author = msg.Author as SocketGuildUser;
                string nick = author.Nickname != null ? author.Nickname + " (" + author.ToString() + ")" : author.ToString();
                string url = author.GetAvatarUrl(ImageFormat.Auto, 32);
                builder.WithAuthor(nick, url);
            }
            else
            {
                builder.WithAuthor(msg.Author);
            }
        }

        public async Task<RestUserMessage> Send(SocketMessage msg, string header = "", string content = "", Color color = new Color(), string picurl = null)
        {
            try
            {
                string username = msg.Author.Username;
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(color);
                builder.WithDescription(content);
                builder.WithFooter(header);
                this.BuilderWithAuthor(msg, builder);

                if (picurl != null)
                {
                    builder.WithThumbnailUrl(picurl);
                }

                if (!string.IsNullOrWhiteSpace(content))
                {
                    return await msg.Channel.SendMessageAsync("", false, builder.Build());
                }
            }
            catch
            {
                this.LogFailedMessage(msg);
            }

            return null;
        }

        public async Task<RestUserMessage> Send(ISocketMessageChannel chan, string header = "", string content = "", Color color = new Color())
        {
            try
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(color);
                builder.WithDescription(content);
                builder.WithFooter(header);

                if (!string.IsNullOrWhiteSpace(content))
                {
                    return await chan.SendMessageAsync("", false, builder.Build());
                }
            }
            catch
            {
                this.LogFailedMessage(chan as SocketChannel);
            }

            return null;
        }

        public async Task<RestUserMessage> Send(SocketMessage msg, Embed embed = null)
        {
            try
            {
                return await msg.Channel.SendMessageAsync("", false, embed);
            }
            catch
            {
                this.LogFailedMessage(msg);
            }

            return null;
        }

        public async Task<RestUserMessage> SendRaw(SocketMessage msg, string content)
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

        public async Task<IUserMessage> Send(SocketChannel chan, Embed embed = null)
        {
            try
            {
                IMessageChannel c = chan as IMessageChannel;
                return await c.SendMessageAsync("", false, embed);
            }
            catch
            {
                this.LogFailedMessage(chan);
            }

            return null;
        }

        public async Task<IUserMessage> Send(RestChannel chan, Embed embed = null)
        {
            try
            {
                IMessageChannel c = chan as IMessageChannel;
                return await c.SendMessageAsync("", false, embed);
            }
            catch
            {
                this.LogFailedMessage(chan);
            }

            return null;
        }

        public async Task<IUserMessage> SendRaw(SocketChannel chan, string content)
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

        public async Task<IUserMessage> RespondByDM(SocketMessage msg, string header = "", string content = "")
        {
            try
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(this.ColorGood);
                builder.WithDescription(content);
                builder.WithFooter(header);
                builder.WithAuthor(msg.Author);

                IDMChannel chan = await msg.Author.GetOrCreateDMChannelAsync();
                return await chan.SendMessageAsync("", false, builder.Build());
            }
            catch
            {
                this.LogFailedMessage(msg);
            }

            return null;
        }

        public async Task<RestUserMessage> Normal(SocketMessage msg, string header, string content)
            => await this.Send(msg, header, content, this.ColorNormal);

        public async Task<RestUserMessage> Normal(SocketChannel chan, string header, string content)
            => await this.Send((chan as ISocketMessageChannel), header, content, this.ColorNormal);

        public async Task<RestUserMessage> Warning(SocketMessage msg, string header, string content)
            => await this.Send(msg, header, content, this.ColorWarning);

        public async Task<RestUserMessage> Warning(SocketChannel chan, string header, string content)
            => await this.Send((chan as ISocketMessageChannel), header, content, this.ColorWarning);

        public async Task<RestUserMessage> Danger(SocketMessage msg, string header, string content)
            => await this.Send(msg, header, content, this.ColorDanger);

        public async Task<RestUserMessage> Danger(SocketChannel chan, string header, string content)
            => await this.Send((chan as ISocketMessageChannel), header, content, this.ColorDanger);

        public async Task<RestUserMessage> Good(SocketMessage msg, string header, string content)
            => await this.Send(msg, header, content, this.ColorGood);

        public async Task<RestUserMessage> Good(SocketChannel chan, string header, string content)
            => await this.Send((chan as ISocketMessageChannel), header, content, this.ColorGood);

        public async Task Disconnect(DiscordSocketClient client)
            => await client.LogoutAsync();

        public async Task<RestUserMessage> SendFile(SocketMessage msg, string path)
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
