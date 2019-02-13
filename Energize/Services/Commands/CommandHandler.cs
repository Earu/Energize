using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Services.Commands
{
    [Service("Commands")]
    public class CommandHandler
    {
        public delegate Task CommandCallback(CommandContext ctx);

        private readonly Dictionary<ulong, CommandCache>  _Caches;
        private readonly CommandCache                     _GlobalCache;

        public CommandHandler(EnergizeClient client)
        {
            this.MessageSender = client.MessageSender;
            this.Commands      = new Dictionary<string, Command>();
            this._Caches       = new Dictionary<ulong, CommandCache>();
            this.Client        = client.Discord;
            this.RESTClient    = client.DiscordREST;
            this.Prefix        = client.Prefix;
            this.Log           = client.Log;
            this._GlobalCache  = new CommandCache();
        }

        public Logger                      Log           { get; }
        public MessageSender             MessageSender { get; }
        public string                      Prefix        { get; }
        public DiscordShardedClient        Client        { get; }
        public Dictionary<string, Command> Commands      { get; }
        public DiscordRestClient           RESTClient    { get; }

        public CommandCache GetChannelCache(ulong id)
        {
            if(this._Caches.TryGetValue(id,out CommandCache cache))
                return cache;
            else
            {
                CommandCache chancache = new CommandCache();
                this._Caches[id] = chancache;

                return chancache;
            }
        }

        public bool IsCmdLoaded(string cmd)
            => this.Commands.ContainsKey(cmd) ? this.Commands[cmd].Loaded : true;

        public void LoadCommand(CommandCallback callback)
        {
            Type cbtype = callback.Target.GetType();
            CommandAttribute att = callback.Method.GetCustomAttributes(typeof(CommandAttribute), false).FirstOrDefault() as CommandAttribute;
            CommandModuleAttribute moduleatt = cbtype.GetCustomAttributes(typeof(CommandModuleAttribute), false).FirstOrDefault() as CommandModuleAttribute;

            if (moduleatt != null && att != null)
            {
                string modulename = moduleatt.Name.ToLower();
                string name = att.Name;
                string help = att.Help;
                string usage = att.Usage;

                if (this.Commands.ContainsKey(name))
                {
                    this.Commands[name].Loaded = true;
                }
                else
                {
                    Command cmd = new Command(name, callback, help, usage, modulename);

                    this.Commands.Add(name, cmd);
                }
            }
        }

        public void UnloadCommand(CommandCallback callback)
        {
            if (callback.Method.GetCustomAttributes(typeof(CommandAttribute), false).FirstOrDefault() is CommandAttribute att)
            {
                string name = att.Name;

                if (this.Commands.ContainsKey(name))
                    this.Commands[name].Loaded = false;
            }
        }

        public bool StartsWithBotMention(string line)
            => Regex.IsMatch(line,$@"^<@!?{this.Client.CurrentUser.Id}>");

        public int GetPrefixLen(string line)
        {
            if(this.StartsWithBotMention(line))
                return this.Client.CurrentUser.Mention.Length;
            else
                return this.Prefix.Length;
        }

        public string GetCmd(string line)
            => line.Substring(this.GetPrefixLen(line)).Split(' ')[0];

        private List<string> GetCmdArgs(string line)
        {
            string str = line.Remove(0, this.GetCmd(line).Length + this.GetPrefixLen(line));
            List<string> results = new List<string>(str.Split(','));
            results[0] = results[0].TrimStart();
            return results;
        }

        private void LogCommand(CommandContext ctx,bool isdeleted=false)
        {
            string log         = string.Empty;
            ConsoleColor color = ConsoleColor.Cyan;
            string head        = "DMCommands";
            string action      = "used";

            if (!ctx.IsPrivate)
            {
                IGuildChannel chan = ctx.Message.Channel as IGuildChannel;
                log  += $"({chan.Guild.Name} - #{chan.Name}) ";
                color = ConsoleColor.Blue;
                head  = "Commands";
            }

            if (isdeleted)
            {
                color  = ConsoleColor.Yellow;
                action = "deleted";
            }

            log += $"{ctx.Message.Author.Username} {action} <{ctx.CommandName}>";
            if (!string.IsNullOrWhiteSpace(ctx.Arguments[0]))
                log += $"  => [ {string.Join(',',ctx.Arguments)} ]";
            else
                log += " with no args";

            this.Log.Nice(head,color, log);
        }

        private CommandContext CreateCmdContext(SocketMessage msg,Command cmd,List<string> args)
        {
            List<SocketGuildUser> users = new List<SocketGuildUser>();
            if(msg.Channel is IGuildChannel)
            {
                IGuildChannel chan = msg.Channel as IGuildChannel;
                foreach(SocketGuildUser u in (chan.Guild as SocketGuild).Users)
                    users.Add(u);
            }

            return new CommandContext
            {
                Client            = this.Client,
                RESTClient        = this.RESTClient,
                Prefix            = this.Prefix,
                MessageSender     = this.MessageSender,
                Message           = msg,
                Command           = cmd,
                Arguments         = args,
                Cache             = this.GetChannelCache(msg.Channel.Id),
                GlobalCache       = this._GlobalCache,
                Log               = this.Log,
                Commands          = this.Commands,
                IsPrivate         = msg.Channel is IDMChannel,
                GuildCachedUsers  = users,
                Handler           = this,
            };
        }

        private async Task CommandCall(SocketMessage msg,string cmd)
        {
            List<string> args = this.GetCmdArgs(msg.Content);
            if (this.Commands.TryGetValue(cmd, out Command retrieved))
            {
                await msg.Channel.TriggerTypingAsync();
                CommandContext ctx = this.CreateCmdContext(msg, retrieved, args);
                Task tcallback = retrieved.Run(ctx);
                if(!tcallback.IsCompleted)
                {
                    Task tres = await Task.WhenAny(tcallback,Task.Delay(20000));
                    if(tres != tcallback)
                    {
                        this.MessageSender.Warning(msg, "Time out", $"Your command `{cmd}` is timing out!")
                            .RunSynchronously();
                        this.Log.Nice("Commands",ConsoleColor.Yellow,$"Time out of command <{cmd}>");
                        await tcallback;
                    }
                }

                this.LogCommand(ctx);
            }
        }

        private async Task MainCall(SocketMessage msg)
        {
            string content = msg.Content;
            if (!msg.Author.IsBot)
            {
                if (content.ToLower().StartsWith(this.Prefix) || this.StartsWithBotMention(content))
                {
                    string cmd = this.GetCmd(content);
                    if (this.IsCmdLoaded(cmd))
                    {
                        await this.CommandCall(msg, cmd).ConfigureAwait(false);
                    }
                    else
                    {
                        this.Log.Nice("Commands", ConsoleColor.Red,$"{msg.Author} tried to use a disabled command <{cmd}>");
                        await this.MessageSender.Warning(msg, "Disabled command", "This is a disabled feature for now");
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
                if(attach.Width.HasValue)
                    url = attach.ProxyUrl;

            IReadOnlyCollection<IEmbed> embeds = msg.Embeds;
            foreach(IEmbed embed in embeds)
                if (embed.Image.HasValue)
                    url = embed.Image.Value.ProxyUrl;

            string pattern = @"(https?:\/\/.+\.(jpg|png|gif))";
            MatchCollection matches = Regex.Matches(msg.Content, pattern);
            if (matches.Count > 0)
                url = matches[matches.Count - 1].Value;

            string giphy = @"https:\/\/giphy\.com\/gifs\/(.+-)?([A-Za-z0-9]+)\s?";
            MatchCollection gifs = Regex.Matches(msg.Content, giphy);
            if(gifs.Count > 0)
            {
                string giftoken = gifs[gifs.Count - 1].Groups[2].Value;
                url = $"https://media.giphy.com/media/{giftoken}/giphy.gif";
            }

            return url;
        }

        /*
         * Service methods 
         * Do not change the signature of those functions
         */
        public void Initialize()
        {
            IEnumerable<Type> Modules = Assembly.GetExecutingAssembly().GetTypes().Where(type => {
                if (type.FullName.StartsWith("Energize.Services.Commands.Modules") && Attribute.IsDefined(type, typeof(CommandModuleAttribute)))
                {
                    CommandModuleAttribute matt = type.GetCustomAttribute(typeof(CommandModuleAttribute)) as CommandModuleAttribute;
                    this.Log.Nice("CMD Module", ConsoleColor.Green, "Initialized " + matt.Name);
                    return true;
                }
                else
                {
                    return false;
                }
            });

            foreach (Type module in Modules)
            {
                object inst = Activator.CreateInstance(module);
                IEnumerable<MethodInfo> methods = module.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                    .Where(method => Attribute.IsDefined(method, typeof(CommandAttribute)));
                foreach (MethodInfo method in methods)
                    this.LoadCommand((CommandCallback)method.CreateDelegate(typeof(CommandCallback), inst));
            }
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            string url = this.GetImageURLS(msg);
            if (url != null)
            {
                this.GetChannelCache(msg.Channel.Id).LastPictureURL = url;
                this._GlobalCache.LastPictureURL = url;
            }

            this.MainCall(msg).RunSynchronously();
        }

        [Event("MessageDeleted")]
        public async Task MessageDeleted(Cacheable<IMessage,ulong> cache,ISocketMessageChannel chan)
        {
            if(cache.HasValue && !cache.Value.Author.IsBot)
            {
                SocketMessage msg = cache.Value as SocketMessage;
                this.GetChannelCache(msg.Channel.Id).LastDeletedMessage = msg;
                this._GlobalCache.LastDeletedMessage = msg;
            }
        }
    }
}
