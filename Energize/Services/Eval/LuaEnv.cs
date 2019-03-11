using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Interfaces.Services.Eval;
using Energize.Toolkit;
using NLua;
using NLua.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Energize.Services.Eval
{
    [Service("Lua")]
    public class LuaEnv : ILuaService
    {
        private readonly DiscordShardedClient _Client;
        private readonly MessageSender        _MessageSender;
        private readonly string               _Path = "External/Lua/SavedScripts";
        private readonly string               _ScriptSeparator = "\n-- GEN --\n";

        private RestApplication _App;

        private readonly Dictionary<ulong, Lua> _States = new Dictionary<ulong, Lua>();

        public LuaEnv(EnergizeClient client)
        {
            this._Client = client.DiscordClient;
            this._MessageSender = client.MessageSender;
        }

        public async Task InitializeAsync()
        {
            _App = await this._Client.GetApplicationInfoAsync();

            if (!Directory.Exists(_Path))
                Directory.CreateDirectory(_Path);

            foreach (string filepath in Directory.GetFiles(_Path))
            {
                string path = filepath.Replace(@"\", "/");
                string[] dir = path.Split("/");
                string id = dir[dir.Length - 1];
                id = id.Remove(id.Length - 4);
                ulong chanid = ulong.Parse(id);

                Lua state = this.CreateState(chanid);
                string script = File.ReadAllText(path);
                string[] parts = script.Split(_ScriptSeparator,StringSplitOptions.RemoveEmptyEntries);
                foreach(string part in parts)
                {
                    state["PART"] = part.Trim();
                    state.DoString(@"sandbox(PART)");
                    state["PART"] = null;
                }
            }
        }

        [Event("UserJoined")]
        public async Task OnUserJoined(SocketGuildUser user)
        {
            IReadOnlyCollection<SocketGuildChannel> channels = user.Guild.Channels;
            foreach(SocketGuildChannel chan in channels)
            {
                if (this._States.ContainsKey(chan.Id))
                {
                    Lua state = this._States[chan.Id];
                    state["USER"] = user as SocketUser;
                    object[] returns = state.DoString(@"return event.fire('OnMemberJoined',USER)");
                    state["USER"] = null;
                    await _MessageSender.Good(chan, "Lua Event", returns[0].ToString());
                }
            }
        }

        [Event("UserLeft")]
        public async Task OnUserLeft(SocketGuildUser user)
        {
            IReadOnlyCollection<SocketGuildChannel> channels = user.Guild.Channels;
            foreach (SocketGuildChannel chan in channels)
            {
                if (this._States.ContainsKey(chan.Id))
                {
                    Lua state = this._States[chan.Id];
                    state["USER"] = user as SocketUser;
                    object[] returns = state.DoString(@"return event.fire('OnMemberLeft',USER)");
                    state["USER"] = null;
                    await _MessageSender.Good(chan, "Lua Event", returns[0].ToString());
                }

            }
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (this._States.ContainsKey(msg.Channel.Id) && msg.Author.Id != _App.Id)
            {
                Lua state = this._States[msg.Channel.Id];
                state["USER"] = msg.Author;
                state["MESSAGE"] = msg;
                object[] returns = state.DoString(@"return event.fire('OnMessageCreated',USER,MESSAGE)");
                state["USER"] = null;
                state["MESSAGE"] = null;
                await _MessageSender.Good((msg.Channel as SocketChannel), "Lua Event", returns[0].ToString());
            }
        }

        [Event("MessageDeleted")]
        public async Task OnMessageDeleted(Cacheable<IMessage,ulong> msg, ISocketMessageChannel c)
        {
            if (msg.HasValue && this._States.ContainsKey(msg.Value.Channel.Id) && msg.Value.Author.Id != _App.Id)
            {
                Lua state = this._States[c.Id];
                state["MESSAGE"] = msg.Value as SocketMessage;
                state["USER"] = msg.Value.Author as SocketUser;
                object[] returns = state.DoString(@"return event.fire('OnMessageDeleted',USER,MESSAGE)");
                state["USER"] = null;
                state["MESSAGE"] = null;
                await _MessageSender.Good((msg.Value.Channel as SocketChannel), "Lua Event", returns[0].ToString());
            }
        }

        [Event("MessageUpdated")]
        public async Task OnMessageUpdated(Cacheable<IMessage, ulong> cache, SocketMessage msg, ISocketMessageChannel c)
        {
            if (this._States.ContainsKey(c.Id) && msg.Author.Id != _App.Id)
            {
                Lua state = this._States[c.Id];
                state["USER"] = msg.Author as SocketUser;
                state["MESSAGE"] = msg as SocketMessage;
                object[] returns = state.DoString(@"return event.fire('OnMessageEdited',USER,MESSAGE)");
                state["USER"] = null;
                state["MESSAGE"] = null;
                await _MessageSender.Good((c as SocketChannel), "Lua Event", returns[0].ToString());
            }
        }

        [Event("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel c, SocketReaction react)
        {
            if(this._States.ContainsKey(c.Id) && react.UserId != _App.Id)
            {
                Lua state = this._States[c.Id];
                state["REACTION"] = react;
                object[] returns = state.DoString(@"return event.fire('OnReactionAdded',REACTION)");
                state["REACTION"] = null;
                await _MessageSender.Good((c as SocketChannel), "Lua Event", returns[0].ToString());
            }
        }

        [Event("ReactionRemoved")]
        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel c, SocketReaction react)
        {
            if (this._States.ContainsKey(c.Id) && react.UserId != _App.Id)
            {
                Lua state = this._States[c.Id];
                state["REACTION"] = react;
                object[] returns = state.DoString(@"return event.fire('OnReactionRemoved',REACTION)");
                state["REACTION"] = null;
                await _MessageSender.Good((c as SocketChannel), "Lua Event", returns[0].ToString());
            }
        }

        private string SafeCode(Lua state, string code)
        {
            state["UNTRUSTED_CODE"] = code;
            code = code.TrimStart();
            return @"local result = sandbox(UNTRUSTED_CODE)
                if result.Success then
                    if result.PrintStack ~= '' then
                        return result.PrintStack,unpack(result.Varargs)
                    else
                        return unpack(result.Varargs)
                    end
                else
                    error(result.Error,0)
                end";
        }

        private Lua CreateState(ulong chanid)
        {
            Lua state = new Lua();
            string sandbox = File.ReadAllText("./External/Lua/Init.lua");
            state.DoString(sandbox);

            return state;
        }

        private void Save(IChannel chan, string code)
        {
            code = code.TrimStart();
            string path =  _Path + "/" + chan.Id + ".lua";
            File.AppendAllText(path,code + _ScriptSeparator);
        }

        public bool Run(IMessage msg, string code, out List<object> returns, out string error, Logger log)
        {
            IChannel chan = msg.Channel as SocketChannel;
            if (!this._States.ContainsKey(chan.Id) || this._States[chan.Id] == null)
                this._States[chan.Id] = CreateState(chan.Id);

            this.Save(chan, code);

            try
            {
                Lua state = this._States[chan.Id];
                code = SafeCode(state,code);

                object[] parts = state.DoString(code,"SANDBOX");
                returns = new List<object>(parts);
                error = "";

                return true;
            }
            catch(LuaException e)
            {
                returns = new List<object>();
                error = e.Message;

                return false;
            }
            catch(Exception e) //no lua return basically
            {
                returns = new List<object>();
                error = e.Message;

                return true;
            }
        }

        public void Reset(ulong chanid, Logger log)
        {
            if (!this._States.ContainsKey(chanid) || this._States[chanid] == null)
                return;

            try
            {
                this._States[chanid].DoString("collectgarbage()");
                this._States[chanid].Close();
                this._States[chanid].Dispose();
                string path = _Path + "/" + chanid + ".lua";
                File.Delete(path);
                this._States.Remove(chanid);
            }
            catch (Exception e)
            {
                log.Nice("LuaEnv", ConsoleColor.Red, $"Could not end a lua state properly: {e.Message}");
            }
    }

        public void Initialize() { }
    }
}
