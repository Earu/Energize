using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Commands
{
    public class CommandReplyEmbed
    {
        private BotLog _Log;
        private CommandHandler _Handler;
        private Color _Good = new Color(30, 30, 30);
        private Color _Normal = new Color(200,200,200);
        private Color _Warning = new Color(226, 123, 68);
        private Color _Danger = new Color(226, 68, 68);

        public BotLog Log { get => this._Log; set => this._Log = value; }
        public CommandHandler Handler { get => this._Handler; set => this._Handler = value; }
        public Color ColorGood { get => this._Good; }
        public Color ColorNormal { get => this._Normal; }
        public Color ColorWarning { get => this._Warning; }
        public Color ColorDanger { get => this._Danger; }

        public async Task BuilderWithAuthor(SocketMessage msg,EmbedBuilder builder)
        {
            if(msg.Channel is SocketGuildChannel)
            {
                SocketGuildUser author = msg.Author as SocketGuildUser;
                string nick = author.Nickname != null ? author.Nickname + " (" + author.ToString() + ")" : author.ToString();
                string url = author.GetAvatarUrl(ImageFormat.Auto,32);
                builder.WithAuthor(nick,url);
            }
            else
            {
                builder.WithAuthor(msg.Author);
            }
        }

        public async Task<RestUserMessage> Send(SocketMessage msg,string header="",string content="",Color color=new Color(),string picurl=null)
        {
            try
            {
                string username = msg.Author.Username;
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(color);
                builder.WithDescription(content);
                builder.WithFooter(header);
                this.BuilderWithAuthor(msg,builder);

                if(picurl != null)
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
                _Log.Nice("EmbedReply", ConsoleColor.Red, "Failed to send a message");
            }

            return null;
        }

        public async Task<RestUserMessage> Send(ISocketMessageChannel chan,string header="",string content="",Color color=new Color())
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
                _Log.Nice("EmbedReply", ConsoleColor.Red, "Failed to send a message");
            }

            return null;
        }

        public async Task<RestUserMessage> Send(SocketMessage msg,Embed embed = null)
        {
            try
            {
                return await msg.Channel.SendMessageAsync("", false, embed);
            }
            catch
            {
                _Log.Nice("EmbedReply", ConsoleColor.Red, "Failed to send a message");
            }

            return null;
        }

        public async Task<RestUserMessage> SendRaw(SocketMessage msg,string content)
        {
            try
            {
                return await msg.Channel.SendMessageAsync(content);
            }
            catch
            {
                _Log.Nice("EmbedReply", ConsoleColor.Red, "Failed to send a message");
            }

            return null;
        }

        public async Task<IUserMessage> Send(SocketChannel chan,Embed embed = null)
        {
            try
            {
                IMessageChannel c = chan as IMessageChannel;
                return await c.SendMessageAsync("", false, embed);
            }
            catch
            {
                _Log.Nice("EmbedReply", ConsoleColor.Red, "Failed to send a message");
            }

            return null;
        }

        public async Task<IUserMessage> SendRaw(SocketChannel chan,string content)
        {
            try
            {
                IMessageChannel c = chan as IMessageChannel;
                return await c.SendMessageAsync(content);
            }
            catch
            {
                _Log.Nice("EmbedReply", ConsoleColor.Red, "Failed to send a message");
            }

            return null;
        }

        public async Task<IUserMessage> RespondByDM(SocketMessage msg,string header="",string content="")
        {
            try
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(this._Good);
                builder.WithDescription(content);
                builder.WithFooter(header);
                builder.WithAuthor(msg.Author);

                IDMChannel chan = await msg.Author.GetOrCreateDMChannelAsync();
                return await chan.SendMessageAsync("", false, builder.Build());
            }
            catch
            {
                _Log.Nice("EmbedReply", ConsoleColor.Red, "Failed to send a message");
            }

            return null;
        }

        public async Task<RestUserMessage> Normal(SocketMessage msg,string header,string content)
        {
            return await this.Send(msg, header, content, this._Normal);
        }

        public async Task<RestUserMessage> Normal(SocketChannel chan, string header, string content)
        {
            return await this.Send((chan as ISocketMessageChannel), header, content, this._Normal);
        }

        public async Task<RestUserMessage> Warning(SocketMessage msg,string header,string content)
        {
            return await this.Send(msg, header, content, this._Warning);
        }

        public async Task<RestUserMessage> Warning(SocketChannel chan, string header, string content)
        {
            return await this.Send((chan as ISocketMessageChannel), header, content, this._Warning);
        }

        public async Task<RestUserMessage> Danger(SocketMessage msg, string header, string content)
        {
            return await this.Send(msg, header, content, this._Danger);
        }

        public async Task<RestUserMessage> Danger(SocketChannel chan, string header, string content)
        {
            return await this.Send((chan as ISocketMessageChannel), header, content, this._Danger);
        }

        public async Task<RestUserMessage> Good(SocketMessage msg, string header, string content)
        {
            return await this.Send(msg, header, content, this._Good);
        }

        public async Task<RestUserMessage> Good(SocketChannel chan,string header,string content)
        {
            return await this.Send((chan as ISocketMessageChannel), header, content, this._Good);
        }

        public async Task Disconnect(DiscordSocketClient client)
        {
            await client.LogoutAsync();
        }

        public async Task<RestUserMessage> SendFile(SocketMessage msg,string path)
        {
            try
            {
                return await msg.Channel.SendFileAsync(path);
            }
            catch
            {
                _Log.Nice("EmbedReply", ConsoleColor.Red, "Failed to send a file");
            }

            return null;
        }
    }
}
