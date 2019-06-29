using System;

namespace Energize.Services
{
    internal class EventAttribute : Attribute
    {
        public EventAttribute(string name)
            => this.Name = name;

        public string Name { get; }
    }
}
