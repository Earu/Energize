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
            return this._Cmds.ContainsKey(cmd) ? this._Cmds[cmd].Loaded : false;
        }

        public void LoadCommand(string name,CommandCallback callback=null,string help=null, string usage=null,string modulename=null)
        {
            if (this._Cmds.ContainsKey(name))
            {
                this._Cmds.Remove(name);
            }
            else
            {
                if (callback == null)
                {
                    callback = async (CommandContext ctx) => 
                    {
                        await ctx.EmbedReply.Good(ctx.Message, null, "Hello world!");
                    };
                }

                Command cmd = new Command(name, callback, help, usage, modulename);

                this._Cmds.Add(name,cmd);
            }
        }

        public void LoadCommand(string[] names,CommandCallback callback=null,string help=null,string usage=null,string modulename=null)
        {
            string cmd = names[0];
            this.LoadCommand(cmd, callback, help, usage, modulename);

            for (uint i = 1; i < names.Length; i++)
            {
                Command.AddAlias(cmd,names[i]);
            }
        }

        public void UnloadCommand(string name)
        {
            if (this._Cmds.ContainsKey(name))
            {
                this._Cmds[name].Loaded = false;
            }
            this._Log.Nice("Commands", ConsoleColor.Red, "Unloaded " + name);
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

        private string GetAliasOriginCmd(string alias)
        {
            Command.Aliases.TryGetValue(alias, out string result);
            return result;
        }

        private void LogCommand(SocketMessage msg, string cmd, List<string> args,bool isdm,bool isdeleted=false)
        {
            string log = "";
            ConsoleColor color = ConsoleColor.Blue;
            string head = "DMCommands";
            string action = "used";

            if (!(msg.Channel is IDMChannel))
            {
                IGuildChannel chan = msg.Channel as IGuildChannel;
                log += "(" + chan.Guild.Name + " - #" + msg.Channel.Name + ") ";
                color = ConsoleColor.Cyan;
                head = "Commands";
            }

            if (isdeleted)
            {
                color = ConsoleColor.Yellow;
                action = "deleted";
            }
            
            log += msg.Author.Username + " " + action + " <" + cmd + ">";
            if (!string.IsNullOrWhiteSpace(args[0]))
            {
                log += "  => [" + string.Join(',',args) + " ]";
            }
            else
            {
                log += " with no args";
            }

            this._Log.Nice(head,color, log);
        }

        private CommandContext CreateCmdContext(SocketMessage msg,string cmd,List<string> args)
        {
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
                Commands = this._Cmds
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
                    await retrieved.Run(this.CreateCmdContext(msg,cmd,args));
                    this.LogCommand(msg, cmd, args, (msg.Channel is IGuildChannel));
                }
                catch (Exception e)
                {
                    this._Log.Nice("Commands", ConsoleColor.Red, "<" + cmd + "> generated an error, args were [" + string.Join(',', args) + " ]");
                    this._Log.Danger(e.ToString());

                    await this._EmbedReply.Danger(msg,msg.Author.Username, "Something went wrong, skipping!");
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
                string cmd = this.GetAliasOriginCmd(this.GetCmd(content));
                if (content.StartsWith(this._Prefix))
                {
                    if (this.IsCmdLoaded(cmd))
                    {
                        this.CommandCall(msg, cmd).RunSynchronously();
                    }
                    else
                    {
                        this._Log.Nice("Commands", ConsoleColor.Red, msg.Author.Username + " tried to use an invalid command <" + cmd + ">");
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

        private async Task GetDeletedCommandMessages(SocketMessage msg)
        {
            string content = msg.Content;
            if (!msg.Author.IsBot)
            {
                string cmd = this.GetAliasOriginCmd(this.GetCmd(content));
                if (content.StartsWith(this._Prefix))
                {
                    if (this.IsCmdLoaded(cmd))
                    {
                        string user = msg.Author.Mention;
                        List<string> args = this.GetCmdArgs(content);

                        await this._EmbedReply.Send(msg.Channel,"Sneaky!",user + " deleted a command message \"" + cmd + "\" :eyes:",new Color(220, 180, 80));
                        this.LogCommand(msg, cmd, args, (msg.Channel is IGuildChannel), true);
                    }
                }
            }
        }

        public void Initialize()
        {
            this._Client.MessageReceived += async msg =>
            {
                this.GetImageURLS(msg);
            };

            this._Client.MessageDeleted += async (msg,chan) =>
            {
                SocketMessage mess = (await msg.DownloadAsync()) as SocketMessage;
                this.GetDeletedCommandMessages(mess).RunSynchronously();
            };

            this._Source.LoadCommands();
            this._Client.MessageReceived += async msg =>
            {
                this.MainCall(msg);
            };
        }
    }
}
