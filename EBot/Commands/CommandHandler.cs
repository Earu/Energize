using Discord;
using Discord.WebSocket;
using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Rest;
using System.Text.RegularExpressions;
using System.Linq;

namespace EBot.Commands
{
    public class CommandHandler
    {
        public delegate Task CommandCallback(CommandContext ctx);

        private DiscordSocketClient _Client;
        private BotLog _Log;
        private string _Prefix;
        private CommandSource _Source;
        private CommandReplyEmbed _EmbedReply;
        private Dictionary<string,  Command> _Cmds;
        private Dictionary<ulong, string> _LastChannelPictureURL;
        public DiscordRestClient _RESTClient;

        public CommandHandler()
        {
            this._EmbedReply = new CommandReplyEmbed
            {
                Handler = this
            };
            this._Cmds = new Dictionary<string, Command>();
            this._LastChannelPictureURL = new Dictionary<ulong, string>();
        }

        public BotLog Log { get => this._Log; set => this._Log = value; }
        public CommandSource Source { get => this._Source; set => this._Source = value; }
        public CommandReplyEmbed EmbedReply { get => this._EmbedReply; set => this._EmbedReply = value; }
        public string Prefix { get => this._Prefix; set => this._Prefix = value; }
        public DiscordSocketClient Client { get => this._Client; set => this._Client = value; }
        public Dictionary<string,Command> Commands { get => this._Cmds; }
        public DiscordRestClient RESTClient { get => this._RESTClient; set => this._RESTClient = value; }

        public string GetLastPictureURL(ulong id)
        {
            if(this._LastChannelPictureURL.TryGetValue(id,out string url))
            {
                return url;
            }
            else
            {
                return null;
            }
        }

        private bool IsCmdLoaded(string cmd)
        {
            return this._Cmds.ContainsKey(cmd) ? this._Cmds[cmd].Loaded : true;
        }

        public void LoadCommand(CommandCallback callback)
        {
            Type cbtype = callback.Target.GetType();
            CommandModuleAttribute matt = cbtype.GetCustomAttributes(typeof(CommandModuleAttribute), false)[0] as CommandModuleAttribute;
            CommandAttribute att = callback.Method.GetCustomAttributes(typeof(CommandAttribute), false)[0] as CommandAttribute;
            string modulename = matt.Name;
            string name = att.Name;
            string help = att.Help;
            string usage = att.Usage;

            if (this._Cmds.ContainsKey(name))
            {
                this._Cmds[name].Loaded = true;
            }
            else
            {
                Command cmd = new Command(name, callback, help, usage, modulename);

                this._Cmds.Add(name,cmd);
            }
        }

        public void UnloadCommand(CommandCallback callback)
        {
            CommandAttribute att = callback.Method.GetCustomAttributes(typeof(CommandAttribute), false)[0] as CommandAttribute;
            string name = att.Name;

            if (this._Cmds.ContainsKey(name))
            {
                this._Cmds[name].Loaded = false;
            }
        }

        private string GetCmd(string line)
        {
            return line.Substring(this._Prefix.Length).Split(' ')[0];
        }

        private List<string> GetCmdArgs(string line)
        {
            string str = line.Remove(0, this.GetCmd(line).Length + this._Prefix.Length);
            return new List<string>(str.Split(','));
        }

        private void LogCommand(CommandContext ctx,bool isdeleted=false)
        {
            string log = "";
            ConsoleColor color = ConsoleColor.Blue;
            string head = "DMCommands";
            string action = "used";

            if (!ctx.IsPrivate)
            {
                IGuildChannel chan = ctx.Message.Channel as IGuildChannel;
                log += "(" + chan.Guild.Name + " - #" + ctx.Message.Channel.Name + ") ";
                color = ConsoleColor.Cyan;
                head = "Commands";
            }

            if (isdeleted)
            {
                color = ConsoleColor.Yellow;
                action = "deleted";
            }
            
            log += ctx.Message.Author.Username + " " + action + " <" + ctx.Command + ">";
            if (!string.IsNullOrWhiteSpace(ctx.Arguments[0]))
            {
                log += "  => [" + string.Join(',',ctx.Arguments) + " ]";
            }
            else
            {
                log += " with no args";
            }

            this._Log.Nice(head,color, log);
        }

        private CommandContext CreateCmdContext(SocketMessage msg,string cmd,List<string> args)
        {
            List<SocketGuildUser> users = new List<SocketGuildUser>();
            if(msg.Channel is IGuildChannel)
            {
                IGuildChannel chan = msg.Channel as IGuildChannel;
                foreach(SocketGuildUser u in (chan.Guild as SocketGuild).Users)
                {
                    users.Add(u);
                }

            }

            CommandContext ctx = new CommandContext
            {
                Client = this._Client,
                RESTClient = this._RESTClient,
                Prefix = this._Prefix,
                EmbedReply = this._EmbedReply,
                Message = msg,
                Command = cmd,
                Arguments = args,
                LastPictureURL = this.GetLastPictureURL(msg.Channel.Id),
                Log = this._Log,
                Commands = this._Cmds,
                IsPrivate = msg.Channel is IDMChannel,
                GuildCachedUsers = users,
                Handler = this
            };

            return ctx;
        }

        private async Task CommandCall(SocketMessage msg,string cmd)
        {
            List<string> args = this.GetCmdArgs(msg.Content);
            if (this._Cmds.TryGetValue(cmd, out Command retrieved))
            {
                try
                {
                    await msg.Channel.TriggerTypingAsync();
                    CommandContext ctx = this.CreateCmdContext(msg, cmd, args);
                    await retrieved.Run(ctx);
                    this.LogCommand(ctx);
                }
                catch (Exception e)
                {
                    this._Log.Nice("Commands", ConsoleColor.Red, "<" + cmd + "> generated an error, args were [" + string.Join(',', args) + " ]");
                    this._Log.Danger(e.ToString());

                    await this._EmbedReply.Danger(msg, "Bad usage", retrieved.GetHelp());
                }
            }
        }

        private async Task MainCall(SocketMessage msg)
        {
            string content = msg.Content;
            if (!msg.Author.IsBot)
            {
                string cmd = this.GetCmd(content);
                if (content.StartsWith(this._Prefix))
                {
                    if (this.IsCmdLoaded(cmd))
                    {
                        await this.CommandCall(msg, cmd);
                    }
                    else
                    {
                        this._Log.Nice("Commands", ConsoleColor.Red, msg.Author.Username + " tried to use an unloaded command <" + cmd + ">");
                        await this._EmbedReply.Warning(msg, "Unloaded Command", "This is not available right now!");
                    }
                }
            }
        }

        private void GetImageURLS(SocketMessage msg)
        {
            string url = null;

            IReadOnlyCollection<Attachment> attachs = msg.Attachments;
            foreach (Attachment attach in attachs)
            {
                if(attach.Width.HasValue)
                {
                    url = attach.ProxyUrl;
                }
            }

            IReadOnlyCollection<Embed> embeds = msg.Embeds;
            foreach(Embed embed in embeds)
            {
                if (embed.Image.HasValue)
                {
                    url = embed.Image.Value.ProxyUrl;
                }
            }

            string pattern = @"(https?:\/\/.+\.(jpg|png|gifv?))";
            MatchCollection matches = Regex.Matches(msg.Content, pattern);
            if (matches.Count > 0)
            {
                url = matches[matches.Count - 1].Value;
            }

            if (url != null)
            {
                this._LastChannelPictureURL[msg.Channel.Id] = url;
            }

        }

        private async Task GetDeletedCommandMessages(SocketMessage msg)
        {
            string content = msg.Content;
            if (!msg.Author.IsBot)
            {
                string cmd = this.GetCmd(content);
                if (content.StartsWith(this._Prefix))
                {
                    if (this.IsCmdLoaded(cmd))
                    {
                        string user = msg.Author.Mention;
                        List<string> args = this.GetCmdArgs(content);

                        await this._EmbedReply.Warning(msg,"Sneaky!", "Deletion of a command message (" + cmd + ") :eyes:");
                        CommandContext ctx = this.CreateCmdContext(msg, cmd, args);
                        this.LogCommand(ctx, true);
                    }
                }
            }
        }

        private async Task OnMessageDeleted(Cacheable<IMessage,ulong> msg,ISocketMessageChannel chan)
        {
            if (msg.HasValue)
            {
                await this.GetDeletedCommandMessages(msg.Value as SocketMessage);
            }
        }

        private async Task OnMessageCreated(SocketMessage msg)
        {
            this.GetImageURLS(msg);
            this.MainCall(msg).RunSynchronously();
        }

        public void Initialize()
        {
            PaginableMessage.Initialize(this._Client);
            this._Client.MessageDeleted += this.OnMessageDeleted;

            this._Source.Initialize();
            this._Client.MessageReceived += this.OnMessageCreated;
        }
    }
}
