namespace Energize.Services
{
    public class Service
    {
        private object _Instance;
        private bool _Initialized;
        private bool _InitializedAsync;
        private string _Name;
        private bool _HasConstructor;

        public Service(object inst)
        {
            this._Instance = inst;
            this._Initialized = false;
            this._InitializedAsync = false;
            this._HasConstructor = false;
        }

        public object Instance { get => this._Instance; }
        public bool Initialized { get => this._Initialized; set => this._Initialized = value; }
        public bool InitializedAsync { get => this._InitializedAsync; set => this._InitializedAsync = value; }
        public string Name { get => this._Name; set => this._Name = value; }
        public bool HasConstructor { get => this._HasConstructor; set => this._HasConstructor = value; }
    }
}
