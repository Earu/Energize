using Discord;
using Discord.WebSocket;
using Energize.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Discord.Rest;
using System.Text.RegularExpressions;
using System.Linq;
using Energize.MachineLearning;
using Energize.MemoryStream;
using System.Reflection;

namespace Energize.Commands
{
    public class CommandHandler
    {
        public delegate Task CommandCallback(CommandContext ctx);

        private DiscordSocketClient              _Client;
        private BotLog                           _Log;
        private string                           _Prefix;
        private CommandReplyEmbed                _EmbedReply;
        private Dictionary<string, Command>      _Cmds;
        private Dictionary<ulong, CommandCache>  _Caches;
        private DiscordRestClient                _RESTClient;

        public CommandHandler()
        {
            this._EmbedReply = new CommandReplyEmbed
            {
                Handler = this
            };
            this._Cmds = new Dictionary<string, Command>();
            this._Caches = new Dictionary<ulong, CommandCache>();
        }

        public BotLog Log                           { get => this._Log;        set => this._Log        = value; }
        public CommandReplyEmbed EmbedReply         { get => this._EmbedReply; set => this._EmbedReply = value; }
        public string Prefix                        { get => this._Prefix;     set => this._Prefix     = value; }
        public DiscordSocketClient Client           { get => this._Client;     set => this._Client     = value; }
        public Dictionary<string,Command> Commands  { get => this._Cmds; }
        public DiscordRestClient RESTClient         { get => this._RESTClient; set => this._RESTClient = value; }

        public CommandCache GetChannelCache(ulong id)
        {
            if(this._Caches.TryGetValue(id,out CommandCache cache))
            {
                return cache;
            }
            else
            {
                CommandCache chancache = new CommandCache();
                this._Caches[id] = chancache;

                return chancache;
            }
        }

        public bool IsCmdLoaded(string cmd)
        {
            return this._Cmds.ContainsKey(cmd) ? this._Cmds[cmd].Loaded : true;
        }

        public void LoadCommand(CommandCallback callback)
        {
            Type cbtype                 = callback.Target.GetType();
            CommandModuleAttribute matt = cbtype.GetCustomAttributes(typeof(CommandModuleAttribute), false)[0] as CommandModuleAttribute;
            CommandAttribute att        = callback.Method.GetCustomAttributes(typeof(CommandAttribute), false)[0] as CommandAttribute;

            string modulename = matt.Name.ToLower();
            string name       = att.Name;
            string help       = att.Help;
            string usage      = att.Usage;

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

        public void LoadCommands()
        {
            IEnumerable<Type> Modules = Assembly.GetExecutingAssembly().GetTypes().Where(type => {
                if(type.FullName.StartsWith("Energize.Commands.Modules") && Attribute.IsDefined(type,typeof(CommandModuleAttribute)))
                {
                    CommandModuleAttribute matt = type.GetCustomAttributes(typeof(CommandModuleAttribute),false)[0] as CommandModuleAttribute;
                    this._Log.Nice("Module",ConsoleColor.Green,"Initialized " + matt.Name);
                    return true;
                }
                else
                {
                    return false;
                }
            });
            
            foreach(Type module in Modules)
            {
                object inst = Activator.CreateInstance(module);
                IEnumerable<MethodInfo> methods = module.GetMethods(BindingFlags.NonPublic|BindingFlags.Instance)
                    .Where(method => Attribute.IsDefined(method,typeof(CommandAttribute)));
                foreach(MethodInfo method in methods)
                {
                    this.LoadCommand((CommandCallback)method.CreateDelegate(typeof(CommandCallback),inst));
                }
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

        public bool StartsWithBotMention(string line)
        {
            if(Regex.IsMatch(line,@"^<@!?" + this._Client.CurrentUser.Id + ">"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int GetPrefixLen(string line)
        {
            if(this.StartsWithBotMention(line))
            {
                return this._Client.CurrentUser.Mention.Length;
            }
            else
            {
                return this._Prefix.Length;
            }
        }

        public string GetCmd(string line)
        {
            return line.Substring(this.GetPrefixLen(line)).Split(' ')[0];
        }

        private List<string> GetCmdArgs(string line)
        {
            string str = line.Remove(0, this.GetCmd(line).Length + this.GetPrefixLen(line));
            List<string> results = new List<string>(str.Split(','));
            results[0] = results[0].TrimStart();
            return results;
        }

        private void LogCommand(CommandContext ctx,bool isdeleted=false)
        {
            string log         = "";
            ConsoleColor color = ConsoleColor.Cyan;
            string head        = "DMCommands";
            string action      = "used";

            if (!ctx.IsPrivate)
            {
                IGuildChannel chan = ctx.Message.Channel as IGuildChannel;
                log  += "(" + chan.Guild.Name + " - #" + ctx.Message.Channel.Name + ") ";
                color = ConsoleColor.Blue;
                head  = "Commands";
            }

            if (isdeleted)
            {
                color  = ConsoleColor.Yellow;
                action = "deleted";
            }

            log += ctx.Message.Author.Username + " " + action + " <" + ctx.Command + ">";
            if (!string.IsNullOrWhiteSpace(ctx.Arguments[0]))
            {
                log += "  => [ " + string.Join(',',ctx.Arguments) + " ]";
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

            return new CommandContext
            {
                Client            = this._Client,
                RESTClient        = this._RESTClient,
                Prefix            = this._Prefix,
                EmbedReply        = this._EmbedReply,
                Message           = msg,
                Command           = cmd,
                Arguments         = args,
                Cache             = this.GetChannelCache(msg.Channel.Id),
                Log               = this._Log,
                Commands          = this._Cmds,
                IsPrivate         = msg.Channel is IDMChannel,
                GuildCachedUsers  = users,
                Handler           = this
            };
        }

        private async Task CommandCall(SocketMessage msg,string cmd)
        {
            List<string> args = this.GetCmdArgs(msg.Content);
            IDisposable state = null;
            if (this._Cmds.TryGetValue(cmd, out Command retrieved))
            {
                try
                {
                    state = msg.Channel.EnterTypingState();
                    await msg.Channel.TriggerTypingAsync();
                    CommandContext ctx = this.CreateCmdContext(msg, cmd, args);
                    Task tcallback = retrieved.Run(ctx,state);
                    if(!tcallback.IsCompleted)
                    {
                        Task tres = await Task.WhenAny(tcallback,Task.Delay(20000));
                        if(tres != tcallback)
                        {
                            this._EmbedReply.Warning(msg, "Time out", "Your command `" + cmd + "` is timing out!");
                            this._Log.Nice("Commands",ConsoleColor.Yellow,"Time out of command <" + cmd + ">");
                            await tcallback;
                        }
                    }

                    this.LogCommand(ctx);
                }
                catch (Exception e)
                {
                    this._Log.Nice("Commands", ConsoleColor.Red, "<" + cmd + "> generated an error, args were [ " + string.Join(',', args) + " ]");
                    this._Log.Danger(e.ToString());
                    state?.Dispose();
                    await this._EmbedReply.Danger(msg, "Bad usage", retrieved.GetHelp());
                }
            }
        }

        private async Task MainCall(SocketMessage msg)
        {
            string content = msg.Content;
            if (!msg.Author.IsBot)
            {
                if (content.ToLower().StartsWith(this._Prefix) || this.StartsWithBotMention(content))
                {
                    string cmd = this.GetCmd(content);
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

            this.GetChannelCache(msg.Channel.Id).LastMessage = msg;
        }

        public string GetImageURLS(IMessage msg)
        {
            string url = null;

            IReadOnlyCollection<IAttachment> attachs = msg.Attachments;
            foreach (IAttachment attach in attachs)
            {
                if(attach.Width.HasValue)
                {
                    url = attach.ProxyUrl;
                }
            }

            IReadOnlyCollection<IEmbed> embeds = msg.Embeds;
            foreach(IEmbed embed in embeds)
            {
                if (embed.Image.HasValue)
                {
                    url = embed.Image.Value.ProxyUrl;
                }
            }

            string pattern = @"(https?:\/\/.+\.(jpg|png|gif))";
            MatchCollection matches = Regex.Matches(msg.Content, pattern);
            if (matches.Count > 0)
            {
                url = matches[matches.Count - 1].Value;
            }

            string giphy = @"https:\/\/giphy\.com\/gifs\/(.+-)?([A-Za-z0-9]+)\s?";
            MatchCollection gifs = Regex.Matches(msg.Content, giphy);
            if(gifs.Count > 0)
            {
                string giftoken = gifs[gifs.Count - 1].Groups[2].Value;
                url = "https://media.giphy.com/media/" + giftoken + "/giphy.gif";
            }

            return url;
        }

        public async Task OnMessageCreated(SocketMessage msg)
        {
            string url = this.GetImageURLS(msg);
            if (url != null)
            {
                this.GetChannelCache(msg.Channel.Id).LastPictureURL = url;
            }

            this.MainCall(msg).RunSynchronously();
        }

        public async Task OnMessageDeleted(Cacheable<IMessage,ulong> cache,ISocketMessageChannel chan)
        {
            if(cache.HasValue)
            {
                SocketMessage msg = cache.Value as SocketMessage;
                this.GetChannelCache(msg.Channel.Id).LastDeletedMessage = msg;
            }
        }
    }
}
