using DSharpPlus.Entities;
using EBot.Logs;
using NLua;
using NLua.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;

namespace EBot.Utils
{
    class LuaEnv
    {
        private static Dictionary<DiscordChannel, Lua> _States = new Dictionary<DiscordChannel, Lua>();

        private static string SafeCode(string code)
        {
            return @"local succ,err,printstack = sandbox([[" + code + @"]])
                if succ then
                    if printstack ~= '' then
                        return printstack,err
                    else
                        return err
                    end
                else
                    error(err,0)
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
                code = SafeCode(code);

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
            _States[chan].Close();
            _States[chan].Dispose();
            _States[chan] = CreateState(chan);
        }
    }
}
