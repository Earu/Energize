using Discord;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Energize.Services.Commands
{
    public class CommandContext
    {
        private delegate SocketUser IdentifierCallback(CommandContext ctx,List<string> args);
        private static Dictionary<string,IdentifierCallback> _Identifiers = new Dictionary<string,IdentifierCallback>
        {
            ["me"] = (ctx,args) => {
                return ctx.Message.Author;
            },
            ["last"] = (ctx,args) => {
                return ctx.Cache.LastMessage.Author;
            },
            ["random"] = (ctx,args) => {
                Random rand = new Random();
                if(ctx.IsPrivate)
                {
                    return rand.Next(0,1) == 1 ? ctx.Message.Author : ctx.Client.CurrentUser as SocketUser;
                }
                else
                {
                    int index = rand.Next(0,ctx.GuildCachedUsers.Count-1);
                    return ctx.GuildCachedUsers[index] as SocketUser;
                }
            },
            ["admin"] = (ctx,args) => {
                Random rand = new Random();
                if(ctx.IsPrivate)
                {
                    return rand.Next(0,1) == 1 ? ctx.Message.Author : ctx.Client.CurrentUser as SocketUser;
                }
                else
                {
                    List<SocketGuildUser> admins = ctx.GuildCachedUsers.Where(x => x.GuildPermissions.Administrator).ToList();
                    return admins[rand.Next(0,admins.Count-1)] as SocketUser;
                }
            },
            ["r"] = (ctx,args) => {
                if(ctx.IsPrivate) return null;
                if(args.Count < 1) return null;

                string role = args[0].ToLower();
                List<SocketGuildUser> results = ctx.GuildCachedUsers.Where(x => x.Roles.Any(r => r.Name.ToLower().Contains(role))).ToList();
                if(results.Count == 0)
                {
                    return null;
                }
                else
                {
                    Random rand = new Random();
                    return results[rand.Next(0,results.Count-1)] as SocketUser;
                }
            },
            ["n"] = (ctx,args) => {
                if(args.Count < 1) return null;

                string name = args[0].ToLower();
                if(ctx.IsPrivate)
                {
                    if(ctx.Message.Author.Username.ToLower() == name)
                    {
                        return ctx.Message.Author;
                    }
                    else if(ctx.Client.CurrentUser.Username.ToLower() == name)
                    {
                        return ctx.Client.CurrentUser as SocketUser;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    List<SocketGuildUser> results = ctx.GuildCachedUsers.Where(x => x.Username != null && x.Username.ToLower() == name).ToList();
                    if(results.Count == 0)
                    {
                        return null;
                    }
                    else
                    {
                        Random rand = new Random();
                        return results[rand.Next(0,results.Count-1)] as SocketUser;
                    }
                }
            },
            ["id"] = (ctx,args) => {
                if(args.Count < 1) return null;

                if(ulong.TryParse(args[0],out ulong id))
                {
                    if(ctx.IsPrivate)
                    {
                        if(ctx.Message.Author.Id == id)
                        {
                            return ctx.Message.Author;
                        }
                        else if(ctx.Client.CurrentUser.Id == id)
                        {
                            return ctx.Client.CurrentUser as SocketUser;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        List<SocketGuildUser> results = ctx.GuildCachedUsers.Where(x => x.Id == id).ToList();
                        if(results.Count == 0)
                        {
                            return null;
                        }
                        else
                        {
                            Random rand = new Random();
                            return results[rand.Next(0,results.Count-1)] as SocketUser;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
        };

        private DiscordSocketClient _Client;
        private DiscordRestClient _RESTClient;
        private Command _Cmd;
        private SocketMessage _Message;
        private List<string> _Args;
        private string _Prefix;
        private EnergizeMessage _MessageSender;
        private EnergizeLog _Log;
        private Dictionary<string, Command> _Cmds;
        private List<SocketGuildUser> _GuildCachedUsers;
        private bool _IsPrivate;
        private CommandHandler _Handler;
        private CommandCache _Cache;
        private CommandCache _GlobalCache;

        public DiscordSocketClient Client             { get => this._Client; set => this._Client = value; }
        public DiscordRestClient RESTClient           { get => this._RESTClient; set => this._RESTClient = value; }
        public Command Command                        { get => this._Cmd; set => this._Cmd = value; }
        public SocketMessage Message                  { get => this._Message; set => this._Message = value; }
        public List<string> Arguments                 { get => this._Args; set => this._Args = value; }
        public string Prefix                          { get => this._Prefix; set => this._Prefix = value; }
        public EnergizeMessage MessageSender          { get => this._MessageSender; set => this._MessageSender = value; }
        public EnergizeLog Log                        { get => this._Log; set => this._Log = value; }
        public Dictionary<string,Command> Commands    { get => this._Cmds; set => this._Cmds = value; }
        public bool IsPrivate                         { get => this._IsPrivate; set => this._IsPrivate = value; }
        public List<SocketGuildUser> GuildCachedUsers { get => this._GuildCachedUsers; set => this._GuildCachedUsers = value; }
        public CommandHandler Handler                 { get => this._Handler; set => this._Handler = value; }
        public CommandCache Cache                     { get => this._Cache; set => this._Cache = value; }
        public bool HasArguments                      { get => (!string.IsNullOrWhiteSpace(this._Args[0])) && this._Args.Count > 0; }
        public string Input                           { get => string.Join(',', this._Args).Trim(); }
        public string AuthorMention                   { get => this._Message.Author.Mention; }
        public string Help                            { get => this._Cmd.GetHelp(); }
        public string CommandName                     { get => this._Cmd.Cmd; }
        public CommandCache GlobalCache               { get => this._GlobalCache; set => this._GlobalCache = value; }

        public bool IsNSFW()
        {
            ITextChannel chan = this._Message.Channel as ITextChannel;
            if (chan.IsNsfw || chan.Name.ToLower().Contains("nsfw"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsAdminUser()
        {
            if (!this._IsPrivate)
            {
                SocketGuildUser user = this._Message.Author as SocketGuildUser;
                List<SocketRole> roles = user.Roles.Where(x => x != null && (x.Name == "EnergizeAdmin" || x.Name == "EBotAdmin")).ToList();
                if(roles.Count > 0 || user.GuildPermissions.Administrator)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool TryGetUser(string input,out SocketUser user,bool withid=false)
        {
            input = input.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(input))
            {
                user = null;
                return false;
            }

            if(this._Message.MentionedUsers.Count > 0)
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
                if(input.StartsWith("$") && input.Length > 1)
                {
                    List<string> args = new List<string>(input.Substring(1,input.Length-1).Split(' '));
                    string identifier = args[0];
                    if(_Identifiers.ContainsKey(identifier))
                    {
                        args.RemoveAt(0);
                        user = _Identifiers[identifier](this,args);
                        return user != null;
                    }
                }

                if (!this._IsPrivate)
                {
                    foreach (SocketGuildUser u in this._GuildCachedUsers)
                    {
                        string nick = u.Nickname ?? u.Username;
                        if (nick != null && nick.ToLower().Contains(input))
                        {
                            user = u;
                            return true;
                        }
                    }
                }

                if(withid && ulong.TryParse(input, out ulong id))
                {
                    SocketUser u = this._Client.GetUser(id);
                    if (u != null)
                    {
                        user = u;
                        return true;
                    }
                }
            }

            user = null;
            return false;
        }
        
        public async Task<IRole> GetOrCreateRole(SocketGuildUser user,string name)
        {
            bool exist = user.Guild.Roles.Any(x => x != null && x.Name == name);
            IRole role = null;

            if (!exist)
            {
                role = await user.Guild.CreateRoleAsync(name);
            }
            else
            {
                role = user.Guild.Roles.Where(x => x != null && x.Name == name).First();
            }

            return role;
        }

        public bool HasRole(SocketGuildUser user,string name)
        {
            return user.Roles.Any(x => x != null && x.Name == name);
        }

        public bool HasRoleStartingWith(SocketGuildUser user,string name)
        {
            return user.Roles.Any(x => x != null && x.Name.StartsWith(name));
        }

        public async Task<RestMessage> SendBadUsage()
        {
            return await this._MessageSender.Warning(this._Message, $"Help [ {Command.Cmd.ToUpper()} ]", this.Help);
        }

        public async Task<bool> IsOwner()
        {
            RestApplication app = await this._RESTClient.GetApplicationInfoAsync();
            if (this._Message.Author.Id != app.Owner.Id)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
