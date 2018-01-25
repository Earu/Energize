using System;
using System.Collections.Generic;
using System.Text;

namespace Energize.Services
{
    public class Service
    {
        private object _Instance;
        private bool _Initialized;
        private bool _InitializedAsync;
        private string _Name;

        public Service(object inst)
        {
            this._Instance = inst;
            this._Initialized = false;
            this._InitializedAsync = false;
        }

        public object Instance { get => this._Instance; }
        public bool Initialized { get => this._Initialized; set => this._Initialized = value; }
        public bool InitializedAsync { get => this._InitializedAsync; set => this._InitializedAsync = value; }
        public string Name { get => this._Name; set => this._Name = value; }
    }
}
