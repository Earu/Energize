using System;
using System.Collections.Generic;
using System.Text;

namespace Energize.Services
{
    class ServiceAttribute : Attribute
    {
        private string _Name;

        public string Name { get => this._Name; set => this._Name = value; }
    }
}
