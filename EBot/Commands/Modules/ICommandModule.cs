using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Text;

namespace EBot.Commands.Modules
{
    interface ICommandModule
    {
        void Setup(CommandsHandler handler, BotLog log);
        void Load();
        void Unload();
    }
}
