using DSharpPlus.Entities;
using EBot.Logs;
using NLua;
using NLua.Exceptions;
using System;
using System.Collections.Generic;

namespace EBot.Utils
{
    class LuaEnv
    {
        private static EBotClient _Client;
        private static Dictionary<DiscordChannel, Lua> _States = new Dictionary<DiscordChannel, Lua>();

        public static void Initialize(EBotClient client)
        {
            _Client = client;
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

        private static Lua CreateState(DiscordChannel chan)
        {
            Lua state = new Lua();
            state.DoFile("External/Lua/Init.lua");
            state.DoString(@"
                ENV.CHANNEL_ID   = tonumber([[" + chan.Id.ToString() + @"]])
                ENV.CHANNEL_NAME = [[" + chan.Name + @"]]
                ENV.GUILD_ID     = tonumber([[" + chan.GuildId + @"]])
                ENV.GUILD_NAME   = [[" + chan.Guild.Name + @"]]
            ", "Startup");

            _Client.Discord.GuildMemberAdded += async e =>
            {
                if(e.Guild == chan.Guild)
                {
                    try
                    {
                        state["NAME"] = e.Member.Username + "#" + e.Member.Discriminator;
                        Object[] returns = state.DoString(@"return event.fire('OnMemberAdded',NAME)");
                        state["NAME"] = null;
                        await _Client.Handler.EmbedReply.Send(chan, "Lua", returns[0].ToString(), new DiscordColor());
                    }
                    catch
                    {

                    }
                }
            };

            _Client.Discord.GuildMemberRemoved += async e =>
            {
                if (e.Guild == chan.Guild)
                {
                    try
                    {
                        state["NAME"] = e.Member.Username + "#" + e.Member.Discriminator;
                        Object[] returns = state.DoString(@"return event.fire('OnMemberRemoved',NAME)");
                        state["NAME"] = null;
                        await _Client.Handler.EmbedReply.Send(chan, "Lua", returns[0].ToString(), new DiscordColor());
                    }
                    catch
                    {

                    }
                }
            };

            _Client.Discord.MessageCreated += async e =>
            {
                if (e.Channel == chan && e.Message.Author.Id != EBotCredentials.BOT_ID_MAIN)
                {
                    try
                    {
                        state["NAME"] = e.Author.Username + "#" + e.Author.Discriminator;
                        state["CONTENT"] = e.Message.Content;
                        Object[] returns = state.DoString(@"return event.fire('OnMessageCreated',NAME,CONTENT)");
                        state["NAME"] = null;
                        state["CONTENT"] = null;
                        await _Client.Handler.EmbedReply.Send(chan, "Lua", returns[0].ToString(), new DiscordColor());
                    }
                    catch{}
                }
            };

            _Client.Discord.MessageDeleted += async e =>
            {
                if (e.Channel == chan && e.Message.Author.Id != EBotCredentials.BOT_ID_MAIN)
                {
                    try
                    {
                        state["CONTENT"] = e.Message.Content;
                        Object[] returns = state.DoString(@"return event.fire('OnMessageDeleted',CONTENT)");
                        state["CONTENT"] = null;
                        await _Client.Handler.EmbedReply.Send(chan, "Lua", returns[0].ToString(), new DiscordColor());
                    }
                    catch{}
                }
            };

            _Client.Discord.MessageUpdated += async e =>
            {
                if (e.Channel == chan && e.Message.Author.Id != EBotCredentials.BOT_ID_MAIN)
                {
                    try
                    {
                        state["NAME"] = e.Author.Username + "#" + e.Author.Discriminator;
                        state["CONTENT"] = e.Message.Content;
                        Object[] returns = state.DoString(@"return event.fire('OnMessageEdited',NAME,CONTENT)");
                        state["NAME"] = null;
                        state["CONTENT"] = null;
                        await _Client.Handler.EmbedReply.Send(chan, "Lua", returns[0].ToString(), new DiscordColor());
                    }catch{}
                }
            };

            return state;
        }

        public static bool Run(DiscordMessage msg,string code,out List<Object> returns,out string error,BotLog log)
        {
            DiscordChannel chan = msg.Channel;
            if (!_States.ContainsKey(chan))
            {
                _States[chan] = CreateState(chan);
            }

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

        public static void Reset(DiscordChannel chan)
        {
            if (_States.ContainsKey(chan))
            {
                _States[chan].DoString("collectgarbage()");
                _States[chan].Close();
                _States[chan].Dispose();
            }
            _States[chan] = CreateState(chan);
        }
    }
}
