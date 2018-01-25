using System;
using System.Collections.Generic;
using System.Text;

namespace Energize.Services
{
    class ServiceAttribute : Attribute
    {
        private string _Name;

        public ServiceAttribute(string name)
        {
            this._Name = name;
        }

        public string Name { get => this._Name; }
    }
}
