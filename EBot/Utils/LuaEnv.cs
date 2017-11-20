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
        private static EBotClient _Client;
        private static RestApplication _App;
        private static Dictionary<SocketChannel, Lua> _States = new Dictionary<SocketChannel, Lua>();
        private static string ScriptSeparator = "\n-- GEN --\n";

        public static async Task Initialize(EBotClient client)
        {
            _Client = client;
            _App = await client.Discord.GetApplicationInfoAsync();
            
            try
            {
                if (!Directory.Exists(_Path))
                {
                    Directory.CreateDirectory(_Path);
                }
            }
            catch (Exception ex)
            {
                BotLog.Debug(ex.Message);
            }

            foreach (string filepath in Directory.GetFiles(_Path))
            {
                string path = filepath.Replace(@"\", "/");
                string[] dir = path.Split("/");
                string id = dir[dir.Length - 1];
                id = id.Remove(id.Length - 4);
                ulong chanid = ulong.Parse(id);
                SocketChannel chan = _Client.Discord.GetChannel(chanid);

                Lua state = CreateState(chan);
                string script = File.ReadAllText(path);
                string[] parts = script.Split(ScriptSeparator);
                foreach(string part in parts)
                {
                    state["SCRIPT"] = part;
                    state.DoString("sandbox(SCRIPT)");
                    state["SCRIPT"] = null;
                }
            }
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

        private static Lua CreateState(SocketChannel chan)
        {
            try
            {
                Lua state = new Lua();
                state.DoFile("External/Lua/Init.lua");
                if (chan is IGuildChannel)
                {
                    IGuildChannel c = chan as IGuildChannel;
                    state.DoString(@"
                    ENV.CHANNEL_ID   = tonumber([[" + chan.Id.ToString() + @"]])
                    ENV.CHANNEL_NAME = [[" + c.Name + @"]]
                    ENV.GUILD_ID     = tonumber([[" + c.GuildId + @"]])
                    ENV.GUILD_NAME   = [[" + c.Guild.Name + @"]]
                ", "Startup");

                    _Client.Discord.UserJoined += async user =>
                    {
                        if (user.Guild == c.Guild)
                        {
                            state["USER"] = user as SocketUser;
                            Object[] returns = state.DoString(@"return event.fire('OnMemberAdded',USER)");
                            state["USER"] = null;
                            await _Client.Handler.EmbedReply.Send((chan as ISocketMessageChannel), "LuaEvent", returns[0].ToString());
                        }
                    };

                    _Client.Discord.UserLeft += async user =>
                    {
                        if (user.Guild == c.Guild)
                        {
                            state["USER"] = user as SocketUser;
                            Object[] returns = state.DoString(@"return event.fire('OnMemberRemoved',USER)");
                            state["USER"] = null;
                            await _Client.Handler.EmbedReply.Send((chan as ISocketMessageChannel), "LuaEvent", returns[0].ToString());
                        }
                    };
                }
                else
                {
                    IDMChannel c = chan as IDMChannel;
                    state.DoString(@"
                    ENV.CHANNEL_ID   = tonumber([[" + c.Id.ToString() + @"]])
                    ENV.CHANNEL_NAME = [[" + c.Name + @"]]
                ", "Startup");
                }

                _Client.Discord.MessageReceived += async msg =>
                {
                    if ((msg.Channel as SocketChannel) == chan && msg.Author.Id != _App.Id)
                    {
                        state["USER"] = msg.Author;
                        state["MESSAGE"] = msg;
                        Object[] returns = state.DoString(@"return event.fire('OnMessageCreated',USER,MESSAGE)");
                        state["USER"] = null;
                        state["MESSAGE"] = null;
                        await _Client.Handler.EmbedReply.Send((chan as ISocketMessageChannel), "LuaEvent", returns[0].ToString());
                    }
                };

                _Client.Discord.MessageDeleted += async (msg, c) =>
                {
                    IMessage mess = await msg.DownloadAsync();
                    if ((c as SocketChannel) == chan && mess.Author.Id != _App.Id)
                    {
                        state["MESSAGE"] = mess as SocketMessage;
                        state["USER"] = mess.Author as SocketUser;
                        Object[] returns = state.DoString(@"return event.fire('OnMessageDeleted',USER,MESSAGE)");
                        state["USER"] = null;
                        state["MESSAGE"] = null;
                        await _Client.Handler.EmbedReply.Send(c, "LuaEvent", returns[0].ToString());
                    }
                };

                _Client.Discord.MessageUpdated += async (cache, msg, c) =>
                {
                    if ((c as SocketChannel) == chan && msg.Author.Id != _App.Id)
                    {
                        state["USER"] = msg.Author as SocketUser;
                        state["MESSAGE"] = msg as SocketMessage;
                        Object[] returns = state.DoString(@"return event.fire('OnMessageEdited',USER,MESSAGE)");
                        state["USER"] = null;
                        state["MESSAGE"] = null;
                        await _Client.Handler.EmbedReply.Send(c, "LuaEvent", returns[0].ToString());
                    }
                };

                return state;
            }catch(LuaException e)
            {
                BotLog.Debug(e.Message);
            }

            return new Lua();
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
            if (!_States.ContainsKey(chan) || _States[chan] == null)
            {
                _States[chan] = CreateState(chan);
            }

            Save(chan, code);

            bool success = true;

            try
            {
                Lua state = _States[chan];
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

        public static void Reset(SocketChannel chan)
        {
            if (_States.ContainsKey(chan))
            {
                _States[chan].DoString("collectgarbage()");
                _States[chan].Close();
                _States[chan].Dispose();
                string path = _Path + "/" + chan.Id + ".lua";
                File.Delete(path);
            }
            _States[chan] = null;
        }
    }
}
