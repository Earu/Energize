using System;

namespace Energize.Services.Commands
{
    class CommandModuleAttribute : Attribute
    {
        private string _Name;

        public CommandModuleAttribute(string name)
        {
            this._Name = name;
        }

        public string Name { get => this._Name; }
    }
}
