using System;

namespace Energize.Services
{
    class EventAttribute : Attribute
    {
        public EventAttribute(string name)
            => this.Name = name;

        public string Name { get; private set; }
    }
}
