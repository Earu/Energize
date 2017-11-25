using Discord;
using Discord.WebSocket;
using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EBot.Commands
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

        public void Send(SocketMessage msg,string header="",string content="",Color color=new Color())
        {
            string username = msg.Author.Username;
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(color);
            builder.WithDescription(content);
            builder.WithFooter(header);
            builder.WithAuthor(msg.Author);

            if (!string.IsNullOrWhiteSpace(content))
            {
                msg.Channel.SendMessageAsync("", false, builder.Build());
            }
        }

        public void Send(ISocketMessageChannel chan,string header="",string content="",Color color=new Color())
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(color);
            builder.WithDescription(content);
            builder.WithFooter(header);

            if (!string.IsNullOrWhiteSpace(content))
            {
                chan.SendMessageAsync("", false, builder.Build());
            }
        }

        public void Send(SocketMessage msg,Embed embed = null)
        {
            msg.Channel.SendMessageAsync("", false, embed);
        }

        public async Task RespondByDM(SocketMessage msg,string header="",string content="")
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(this._Good);
            builder.WithDescription(content);
            builder.WithFooter(header);
            builder.WithAuthor(msg.Author);

            IDMChannel chan = await msg.Author.GetOrCreateDMChannelAsync();
            chan.SendMessageAsync("",false,builder.Build());
        }

        public void Normal(SocketMessage msg,string header,string content)
        {
            this.Send(msg, header, content, this._Normal);
        }

        public void Normal(SocketChannel chan, string header, string content)
        {
            this.Send((chan as ISocketMessageChannel), header, content, this._Normal);
        }

        public void Warning(SocketMessage msg,string header,string content)
        {
            this.Send(msg, header, content, this._Warning);
        }

        public void Warning(SocketChannel chan, string header, string content)
        {
            this.Send((chan as ISocketMessageChannel), header, content, this._Warning);
        }

        public void Danger(SocketMessage msg, string header, string content)
        {
            this.Send(msg, header, content, this._Danger);
        }

        public void Danger(SocketChannel chan, string header, string content)
        {
            this.Send((chan as ISocketMessageChannel), header, content, this._Danger);
        }

        public void Good(SocketMessage msg, string header, string content)
        {
            this.Send(msg, header, content, this._Good);
        }

        public void Good(SocketChannel chan,string header,string content)
        {
            this.Send((chan as ISocketMessageChannel), header, content, this._Good);
        }

        public async Task Disconnect(DiscordSocketClient client)
        {
            await client.LogoutAsync();
        }

        public async Task MakePaginable(SocketMessage msg,List<Object> list,Action<PaginableMessage,EmbedBuilder> callback)
        {
            PaginableMessage page = new PaginableMessage();
            await page.Setup(msg, list, callback);
        }
    }
}
