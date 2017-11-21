using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Text;

namespace EBot.Commands.Modules
{
    interface ICommandModule
    {
        void Load(CommandHandler handler,BotLog log);
        void Unload(CommandHandler handler,BotLog log);
    }
}
