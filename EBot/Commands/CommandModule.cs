using EBot.Logs;
using System;

namespace EBot.Commands
{
    public class CommandModule
    {
        public string GetModuleName()
        {
            Type type = this.GetType();
            CommandModuleAttribute att = type.GetCustomAttributes(typeof(CommandModuleAttribute), true)[0] as CommandModuleAttribute;
            return att.Name;
        }
    }
}
