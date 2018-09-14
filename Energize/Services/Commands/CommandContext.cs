using Discord;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Energize.Services.Commands
{
    public class CommandContext
    {
        private delegate SocketUser ArgumentCallback(CommandContext ctx, List<string> args);

        private static Dictionary<string,ArgumentCallback> LoadArguments()
        {
            Type type = typeof(CommandArguments);   
            Type attributetype = typeof(CommandArgAttribute);
            object inst = Activator.CreateInstance(type);
            IEnumerable<MethodInfo> methods = type.GetMethods()
                .Where(method => Attribute.IsDefined(method, attributetype));

            Dictionary<string, ArgumentCallback> callbacks = new Dictionary<string, ArgumentCallback>();
            foreach(MethodInfo method in methods)
            {
                CommandArgAttribute arg = method.GetCustomAttribute(attributetype) as CommandArgAttribute;
                callbacks[arg.Identifier] = (ArgumentCallback)method.CreateDelegate(typeof(ArgumentCallback), inst);
            }

            return callbacks;
        }

        private static Dictionary<string, ArgumentCallback> _ArgumentCallbacks = LoadArguments();

        private DiscordSocketClient         _Client;
        private DiscordRestClient           _RESTClient;
        private Command                     _Cmd;
        private SocketMessage               _Message;
        private List<string>                _Args;
        private string                      _Prefix;
        private EnergizeMessage             _MessageSender;
        private EnergizeLog                 _Log;
        private Dictionary<string, Command> _Cmds;
        private List<SocketGuildUser>       _GuildCachedUsers;
        private bool                        _IsPrivate;
        private CommandHandler              _Handler;
        private CommandCache                _Cache;
        private CommandCache                _GlobalCache;

        public DiscordSocketClient        Client           { get => this._Client;           set => this._Client           = value; }
        public DiscordRestClient          RESTClient       { get => this._RESTClient;       set => this._RESTClient       = value; }
        public Command                    Command          { get => this._Cmd;              set => this._Cmd              = value; }
        public SocketMessage              Message          { get => this._Message;          set => this._Message          = value; }
        public List<string>               Arguments        { get => this._Args;             set => this._Args             = value; }
        public string                     Prefix           { get => this._Prefix;           set => this._Prefix           = value; }
        public EnergizeMessage            MessageSender    { get => this._MessageSender;    set => this._MessageSender    = value; }
        public EnergizeLog                Log              { get => this._Log;              set => this._Log              = value; }
        public Dictionary<string,Command> Commands         { get => this._Cmds;             set => this._Cmds             = value; }
        public bool                       IsPrivate        { get => this._IsPrivate;        set => this._IsPrivate        = value; }
        public List<SocketGuildUser>      GuildCachedUsers { get => this._GuildCachedUsers; set => this._GuildCachedUsers = value; }
        public CommandHandler             Handler          { get => this._Handler;          set => this._Handler          = value; }
        public CommandCache               Cache            { get => this._Cache;            set => this._Cache            = value; }
        public CommandCache               GlobalCache      { get => this._GlobalCache;      set => this._GlobalCache      = value; }

        public bool                       HasArguments     { get => (!string.IsNullOrWhiteSpace(this._Args[0])) && this._Args.Count > 0; }
        public string                     Input            { get => string.Join(',', this._Args).Trim();                                 }
        public string                     AuthorMention    { get => this._Message.Author.Mention;                                        }
        public string                     Help             { get => this._Cmd.GetHelp();                                                 }
        public string                     CommandName      { get => this._Cmd.Cmd;                                                       }

        public bool IsNSFW()
        {
            ITextChannel chan = this._Message.Channel as ITextChannel;
            return (chan.IsNsfw || chan.Name.ToLower().Contains("nsfw"));
        }

        public bool IsAdminUser()
        {
            if (!this._IsPrivate)
            {
                SocketGuildUser user = this._Message.Author as SocketGuildUser;
                List<SocketRole> roles = user.Roles.Where(x => x != null && (x.Name == "EnergizeAdmin" || x.Name == "EBotAdmin")).ToList();
                return (roles.Count > 0 || user.GuildPermissions.Administrator);
            }

            return false;
        }

        private SocketUser FindUser(string input,bool withid=false)
        {
            if (input.StartsWith("$") && input.Length > 1)
            {
                List<string> args = new List<string>(input.Substring(1, input.Length - 1).Split(' '));
                string identifier = args[0];
                if (_ArgumentCallbacks.ContainsKey(identifier))
                {
                    args.RemoveAt(0);
                    return _ArgumentCallbacks[identifier](this, args);
                }
            }

            if (!this._IsPrivate)
                foreach (SocketGuildUser u in this._GuildCachedUsers)
                {
                    string nick = u.Nickname ?? u.Username;
                    if (nick != null && nick.ToLower().Contains(input)) return u;
                }

            if (withid && ulong.TryParse(input, out ulong id))
            {
                SocketUser u = this._Client.GetUser(id);
                if (u != null) return u;
            }

            return null;
        }

        public bool TryGetUser(string input,out SocketUser user,bool withid=false)
        {
            input = input.Trim().ToLower();
            user = null;

            if (string.IsNullOrWhiteSpace(input)) return false;

            if (this._Message.MentionedUsers.Count > 0)
            {
                foreach (SocketUser u in this._Message.MentionedUsers)
                {
                    if (input.Contains(@"<@" + u.Id + ">") || input.Contains(@"<@!" + u.Id + ">"))
                    {
                        user = u;
                        return true;
                    }
                }
            }
            else
            {
                user = this.FindUser(input, withid);
                return user != null;
            }

            return false;
        }
        
        public async Task<IRole> GetOrCreateRole(SocketGuildUser user,string name)
        {
            bool exist = user.Guild.Roles.Any(x => x != null && x.Name == name);
            IRole role = null;

            if (!exist)
                role = await user.Guild.CreateRoleAsync(name);
            else
                role = user.Guild.Roles.Where(x => x != null && x.Name == name).First();

            return role;
        }

        public bool HasRole(SocketGuildUser user,string name) => user.Roles.Any(x => x != null && x.Name == name);
        public bool HasRoleStartingWith(SocketGuildUser user,string name) => user.Roles.Any(x => x != null && x.Name.StartsWith(name));

        public async Task<RestMessage> SendBadUsage(string extra=null)
        {
            string help = $"{this.Help}**EXTRA:**\n{extra}";
            return await this._MessageSender.Warning(this._Message, $"Help [ {Command.Cmd.ToUpper()} ]", help);
        }

        public async Task<bool> IsOwner()
        {
            RestApplication app = await this._RESTClient.GetApplicationInfoAsync();
            return !(this._Message.Author.Id != app.Owner.Id);
        }
    }
}
