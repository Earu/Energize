using System;
using System.Collections.Generic;
using System.Text;

namespace Energize.Services
{
    class EventAttribute : Attribute
    {
        private string _Name;

        public EventAttribute(string name)
        {
            this._Name = name;
        }

        public string Name { get => this._Name; }
    }
}
