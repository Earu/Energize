using System;

namespace Energize.Services
{
    internal class ServiceAttribute : Attribute
    {
        public ServiceAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }
}
