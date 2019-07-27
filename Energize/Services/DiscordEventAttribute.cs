using System;

namespace Energize.Services
{
    internal class DiscordEventAttribute : Attribute
    {
        public DiscordEventAttribute(string name, bool maintenanceImpl = false)
        {
            this.Name = name;
            this.MaintenanceImplementation = maintenanceImpl;
        }

        public string Name { get; }
        public bool MaintenanceImplementation { get; }
    }
}
