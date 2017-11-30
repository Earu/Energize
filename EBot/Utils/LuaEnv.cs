using Discord;
using Discord.Rest;
using Discord.WebSocket;
using EBot.Logs;
using NLua;
using NLua.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace EBot.Utils
{
    class LuaEnv
    {
        private static string _Path = "External/Lua/SavedScripts";
        private static RestApplication _App;
        private static Dictionary<ulong, Lua> _States = new Dictionary<ulong, Lua>();
        private static string ScriptSeparator = "\n-- GEN --\n";
        public static Func<SocketGuildUser,Task> OnUserLeft;
        public static Func<SocketGuildUser, Task> OnUserJoined;
        public static Func<SocketMessage, Task> OnMessageReceived;
        public static Func<Cacheable<IMessage, UInt64>, ISocket​Message​Channel, Task> OnMessageDeleted;
        public static Func<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel, Task> OnMessageUpdated;
        public static Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> OnReactionAdded;
        public static Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> OnReactionRemoved;

        public static async Task InitializeAsync(EBotClient client)
        {
            _App = await client.Discord.GetApplicationInfoAsync();

            if (!Directory.Exists(_Path))
            {
                Directory.CreateDirectory(_Path);
            }

            foreach (string filepath in Directory.GetFiles(_Path))
            {
                string path = filepath.Replace(@"\", "/");
                string[] dir = path.Split("/");
                string id = dir[dir.Length - 1];
                id = id.Remove(id.Length - 4);
                ulong chanid = ulong.Parse(id);

                Lua state = CreateState(chanid);
                string script = File.ReadAllText(path);
                string[] parts = script.Split(ScriptSeparator,StringSplitOptions.RemoveEmptyEntries);
                foreach(string part in parts)
                {
                    state["PART"] = part.Trim();
                    state.DoString(@"sandbox(PART)");
                    state["PART"] = null;
                }
            }

            OnUserJoined = async user =>
            {
                IReadOnlyList<SocketGuildChannel> channels = user.Guild.Channels as IReadOnlyList<SocketGuildChannel>;
                foreach(SocketGuildChannel chan in channels)
                {
                    if (_States.ContainsKey(chan.Id))
                    {
                        Lua state = _States[chan.Id];
                        state["USER"] = user as SocketUser;
                        Object[] returns = state.DoString(@"return event.fire('OnMemberJoined',USER)");
                        state["USER"] = null;
                        await client.Handler.EmbedReply.Good(chan, "Lua Event", returns[0].ToString());
                    }
                }
            };

            OnUserLeft = async user =>
            {
                IReadOnlyList<SocketGuildChannel> channels = user.Guild.Channels as IReadOnlyList<SocketGuildChannel>;
                foreach (SocketGuildChannel chan in channels)
                {
                    if (_States.ContainsKey(chan.Id))
                    {
                        Lua state = _States[chan.Id];
                        state["USER"] = user as SocketUser;
                        Object[] returns = state.DoString(@"return event.fire('OnMemberLeft',USER)");
                        state["USER"] = null;
                        await client.Handler.EmbedReply.Good(chan, "Lua Event", returns[0].ToString());
                    }

                }
            };

            OnMessageReceived = async msg =>
            {
                if (_States.ContainsKey(msg.Channel.Id) && msg.Author.Id != _App.Id)
                {
                    Lua state = _States[msg.Channel.Id];
                    state["USER"] = msg.Author;
                    state["MESSAGE"] = msg;
                    Object[] returns = state.DoString(@"return event.fire('OnMessageCreated',USER,MESSAGE)");
                    state["USER"] = null;
                    state["MESSAGE"] = null;
                    await client.Handler.EmbedReply.Good((msg.Channel as SocketChannel), "Lua Event", returns[0].ToString());
                }
            };

            OnMessageDeleted = async (msg, c) =>
            {
                if (msg.HasValue && _States.ContainsKey(msg.Value.Channel.Id) && msg.Value.Author.Id != _App.Id)
                {
                    Lua state = _States[c.Id];
                    state["MESSAGE"] = msg.Value as SocketMessage;
                    state["USER"] = msg.Value.Author as SocketUser;
                    Object[] returns = state.DoString(@"return event.fire('OnMessageDeleted',USER,MESSAGE)");
                    state["USER"] = null;
                    state["MESSAGE"] = null;
                    await client.Handler.EmbedReply.Good((msg.Value.Channel as SocketChannel), "Lua Event", returns[0].ToString());
                }
            };

            OnMessageUpdated = async (cache, msg, c) =>
            {
                if (_States.ContainsKey(c.Id) && msg.Author.Id != _App.Id)
                {
                    Lua state = _States[c.Id];
                    state["USER"] = msg.Author as SocketUser;
                    state["MESSAGE"] = msg as SocketMessage;
                    Object[] returns = state.DoString(@"return event.fire('OnMessageEdited',USER,MESSAGE)");
                    state["USER"] = null;
                    state["MESSAGE"] = null;
                    await client.Handler.EmbedReply.Good((c as SocketChannel), "Lua Event", returns[0].ToString());
                }
            };

            OnReactionAdded = async (cache,c,react) => 
            {
                if(_States.ContainsKey(c.Id) && react.UserId != _App.Id)
                {
                    Lua state = _States[c.Id];
                    state["REACTION"] = react;
                    Object[] returns = state.DoString(@"return even.fire('OnReactionAdded',REACTION)");
                    state["REACTION"] = null;
                    await client.Handler.EmbedReply.Good((c as SocketChannel), "Lua Event", returns[0].ToString());
                }
            };

            OnReactionRemoved = async (cache, c, react) =>
            {
                if (_States.ContainsKey(c.Id) && react.UserId != _App.Id)
                {
                    Lua state = _States[c.Id];
                    state["REACTION"] = react;
                    Object[] returns = state.DoString(@"return even.fire('OnReactionRemoved',REACTION)");
                    state["REACTION"] = null;
                    await client.Handler.EmbedReply.Good((c as SocketChannel), "Lua Event", returns[0].ToString());
                }
            };
        }

        private static string SafeCode(Lua state,string code)
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

        private static Lua CreateState(ulong chanid)
        {
            Lua state = new Lua();
            string sandbox = File.ReadAllText("./External/Lua/Init.lua");
            state.DoString(sandbox);

            return state;
        }

        private static void Save(SocketChannel chan,string code)
        {
            code = code.TrimStart();
            string path =  _Path + "/" + chan.Id + ".lua";
            File.AppendAllText(path,code + ScriptSeparator);
        }

        public static bool Run(SocketMessage msg,string code,out List<Object> returns,out string error,BotLog log)
        {
            SocketChannel chan = msg.Channel as SocketChannel;
            if (!_States.ContainsKey(chan.Id) || _States[chan.Id] == null)
            {
                _States[chan.Id] = CreateState(chan.Id);
            }

            Save(chan, code);

            bool success = true;

            try
            {
                Lua state = _States[chan.Id];
                code = SafeCode(state,code);

                Object[] parts = state.DoString(code,"SANDBOX");
                returns = new List<object>(parts);
                error = "";
            }
            catch(LuaException e)
            {
                returns = new List<object>();
                error = e.Message;
                success = false;
            }
            catch(Exception e) //no lua return basically
            {
                returns = new List<object>();
                error = e.Message;
                success = true;
            }

            return success;
        }

        public static void Reset(ulong chanid,BotLog log)
        {
            if (_States.ContainsKey(chanid))
            {
                try
                {
                    _States[chanid].DoString("collectgarbage()");
                }
                catch
                {
                    log.Nice("LuaEnv", ConsoleColor.Red, "The state couldn't call collectgarbage()");
                }
                _States[chanid].Close();
                _States[chanid].Dispose();
                string path = _Path + "/" + chanid + ".lua";
                File.Delete(path);
            }
            _States[chanid] = null;
        }
    }
}
