using System;

namespace Energize.Services.Commands
{
    [AttributeUsage(AttributeTargets.Method)]
    class CommandAttribute : Attribute
    {
        private string _Name;
        private string _Usage;
        private string _Help;

        public string Name { get => this._Name; set => this._Name = value; }
        public string Usage { get => this._Usage; set => this._Usage = value; }
        public string Help { get => this._Help; set => this._Help = value; }
    }
}