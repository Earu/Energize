using System;
using System.Collections.Generic;
using System.Text;

namespace Energize.Services.Commands
{
    public class CommandArgAttribute : Attribute
    {
        private string _Identifier;

        public CommandArgAttribute(string name)
        {
            this._Identifier = name;
        }

        public string Identifier { get => this._Identifier; }
    }
}
