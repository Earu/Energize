using System;

namespace Energize.Services
{
    class ServiceAttribute : Attribute
    {
        public ServiceAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }
    }
}
