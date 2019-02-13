using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Toolkit;
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

        public DiscordShardedClient        Client           { get; set; }
        public DiscordRestClient           RESTClient       { get; set; }
        public Command                     Command          { get; set; }
        public SocketMessage               Message          { get; set; }
        public List<string>                Arguments        { get; set; }
        public string                      Prefix           { get; set; }
        public MessageSender               MessageSender    { get; set; }
        public Logger                      Log              { get; set; }
        public Dictionary<string, Command> Commands         { get; set; }
        public bool                        IsPrivate        { get; set; }
        public List<SocketGuildUser>       GuildCachedUsers { get; set; }
        public CommandHandler              Handler          { get; set; }
        public CommandCache                Cache            { get; set; }
        public CommandCache                GlobalCache      { get; set; }

        public bool   HasArguments     { get => (!string.IsNullOrWhiteSpace(this.Arguments[0])) && this.Arguments.Count > 0; }
        public string Input            { get => string.Join(',', this.Arguments).Trim();                                     }
        public string AuthorMention    { get => this.Message.Author.Mention;                                                 }
        public string Help             { get => this.Command.GetHelp();                                                      }
        public string CommandName      { get => this.Command.Cmd;                                                            }

        public bool IsNSFW()
        {
            ITextChannel chan = this.Message.Channel as ITextChannel;
            return (chan.IsNsfw || chan.Name.ToLower().Contains("nsfw"));
        }

        public bool IsAdminUser()
        {
            if (!this.IsPrivate)
            {
                SocketGuildUser user = this.Message.Author as SocketGuildUser;
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

            if (!this.IsPrivate)
                foreach (SocketGuildUser u in this.GuildCachedUsers)
                {
                    string nick = u.Nickname ?? u.Username;
                    if (nick != null && nick.ToLower().Contains(input)) return u;
                }

            if (withid && ulong.TryParse(input, out ulong id))
            {
                SocketUser u = this.Client.GetUser(id);
                if (u != null) return u;
            }

            return null;
        }

        public bool TryGetUser(string input,out SocketUser user,bool withid=false)
        {
            input = input.Trim().ToLower();
            user = null;

            if (string.IsNullOrWhiteSpace(input)) return false;

            if (this.Message.MentionedUsers.Count > 0)
            {
                foreach (SocketUser u in this.Message.MentionedUsers)
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
            return await this.MessageSender.Warning(this.Message, $"Help [ {Command.Cmd.ToUpper()} ]", help);
        }

        public async Task<bool> IsOwner()
        {
            RestApplication app = await this.RESTClient.GetApplicationInfoAsync();
            return !(this.Message.Author.Id != app.Owner.Id);
        }
    }
}
