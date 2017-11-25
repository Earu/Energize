using Discord;
using Discord.WebSocket;
using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Rest;

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
        private Dictionary<string, string> _LastChannelPictureURL;
        public DiscordRestClient _RESTClient;

        public CommandHandler()
        {
            this._EmbedReply = new CommandReplyEmbed
            {
                Handler = this
            };
            this._Cmds = new Dictionary<string, Command>();
            this._LastChannelPictureURL = new Dictionary<string, string>();
        }

        public BotLog Log { get => this._Log; set => this._Log = value; }
        public CommandSource Source { get => this._Source; set => this._Source = value; }
        public CommandReplyEmbed EmbedReply { get => this._EmbedReply; set => this._EmbedReply = value; }
        public string Prefix { get => this._Prefix; set => this._Prefix = value; }
        public DiscordSocketClient Client { get => this._Client; set => this._Client = value; }
        public Dictionary<string,Command> Commands { get => this._Cmds; }
        public DiscordRestClient RESTClient { get => this._RESTClient; set => this._RESTClient = value; }

        public string GetLastPictureURL(SocketChannel chan)
        {
            string index = chan.Id.ToString();
            return this._LastChannelPictureURL.TryGetValue(index, out string url) ? url : "";
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
                LastPictureURL = this._LastChannelPictureURL.TryGetValue(msg.Channel.ToString(), out string last) ? last : "",
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
            bool success = this._Cmds.TryGetValue(cmd, out Command retrieved);

            if (success)
            {
                try
                {
                    await msg.Channel.TriggerTypingAsync();
                    CommandContext ctx = this.CreateCmdContext(msg, cmd, args);
                    await retrieved.Run(ctx);
                    this.LogCommand(ctx);
                }
                catch (Discord.Net.HttpException e)
                {
                    string log = "";
                    if(msg.Channel is IGuildChannel)
                    {
                        IGuildChannel chan = msg.Channel as IGuildChannel;
                        log += log += chan.Guild.Name + " - "; 
                    }
                    log += "#" + msg.Channel.Name;
                    this._Log.Nice("Commands", ConsoleColor.Red, "( " + log + " ) Reply was blocked");
                }
                catch (Exception e)
                {
                    this._Log.Nice("Commands", ConsoleColor.Red, "<" + cmd + "> generated an error, args were [" + string.Join(',', args) + " ]");
                    this._Log.Danger(e.ToString());

                    this._EmbedReply.Danger(msg, "Bad usage", retrieved.GetHelp());
                }
            }
            else
            {
                this._Log.Nice("Commands", ConsoleColor.Red, "No callback for <" + cmd + ">");
            }
        }

        private void MainCall(SocketMessage msg)
        {
            string content = msg.Content;
            if (!msg.Author.IsBot)
            {
                string cmd = this.GetCmd(content);
                if (content.StartsWith(this._Prefix))
                {
                    if (this.IsCmdLoaded(cmd))
                    {
                        this.CommandCall(msg, cmd);
                    }
                    else
                    {
                        this._Log.Nice("Commands", ConsoleColor.Red, msg.Author.Username + " tried to use an unloaded command <" + cmd + ">");
                        this._EmbedReply.Warning(msg, "Unloaded Command", "This is not available right now!");
                    }
                }
            }
        }

        private void CacheURLs(SocketMessage msg,List<string> urls)
        {
            if (urls.Count > 0)
            {
                string index = msg.Channel.Id.ToString();
                string lasturl = urls[urls.Count - 1];

                if (this._LastChannelPictureURL.ContainsKey(index))
                {
                    this._LastChannelPictureURL.Remove(index);
                }
                this._LastChannelPictureURL.Add(index, lasturl);
            }
        }

        private void GetImageURLS(SocketMessage msg)
        {
            List<string> urls = new List<string>();

            IReadOnlyCollection<Attachment> attachs = msg.Attachments;
            foreach (Attachment attach in attachs)
            {
                if(attach.Filename.EndsWith(".jpg") || attach.Filename.EndsWith(".jpeg") || attach.Filename.EndsWith(".png"))
                {
                    urls.Add(attach.Url);
                }
            }

            IReadOnlyCollection<Embed> embeds = msg.Embeds;
            foreach(Embed embed in embeds)
            {
                if (embed.Type == EmbedType.Image)
                {
                    string url = embed.Url;
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        urls.Add(url);
                    }

                }else if(embed.Type == EmbedType.Rich)
                {
                    string url = embed.Url;
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        urls.Add(url);
                    }
                }
            }

            this.CacheURLs(msg,urls);

        }

        private void GetDeletedCommandMessages(SocketMessage msg)
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

                        this._EmbedReply.Warning(msg,"Sneaky!", "Deletion of a command message (" + cmd + ") :eyes:");
                        CommandContext ctx = this.CreateCmdContext(msg, cmd, args);
                        this.LogCommand(ctx, true);
                    }
                }
            }
        }

        public void Initialize()
        {
            PaginableMessage.Initialize(this._Client);
            this._Client.MessageDeleted += async (msg,chan) =>
            {
                if (msg.HasValue)
                {
                    this.GetDeletedCommandMessages(msg.Value as SocketMessage);
                }
            };

            this._Source.Initialize();
            this._Client.MessageReceived += async msg =>
            {
                this.MainCall(msg);
                this.GetImageURLS(msg);
            };
        }
    }
}
