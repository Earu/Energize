using System;

namespace Energize.Commands
{
    class CommandModuleAttribute : Attribute
    {
        private string _Name;

        public string Name { get => this._Name; set => this._Name = value; }
    }
}
