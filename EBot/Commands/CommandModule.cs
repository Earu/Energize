using System;
using System.Collections.Generic;
using System.Text;

namespace EBot.Commands
{
    class CommandModule
    {
        public string GetModuleName()
        {
            Type type = this.GetType();
            CommandModuleAttribute att = type.GetCustomAttributes(typeof(CommandModuleAttribute), true)[0] as CommandModuleAttribute;
            return att.Name;
        }
    }
}
