using Discord.WebSocket;
using System.Collections.Generic;

namespace Energize.Drama
{
    class DramaInfo
    {
        private int _Drama;
        private List<SocketUser> _Watcheds;
        private List<SocketUser> _Users;
        private List<string> _SensibleWords;
        private List<string> _ApologizeWors;
        private List<string> _NormalWords;
        private bool _IsCaps;
        private double _Factor;

        public int Drama { get => this._Drama; set => this._Drama = value; }
        public List<SocketUser> Watcheds { get => this._Watcheds; set => this._Watcheds = value; }
        public List<SocketUser> Users { get => this._Users; set => this._Users = value; }
        public List<string> SensibleWords { get => this._SensibleWords; set => this._SensibleWords = value; }
        public List<string> ApologizeWords { get => this._ApologizeWors; set => this._ApologizeWors = value; }
        public List<string> NormalWords { get => this._NormalWords; set => this._NormalWords = value; }
        public bool IsCaps { get => this._IsCaps; set => this._IsCaps = value; }
        public double Factor { get => this._Factor; set => this._Factor = value; }

    }
}
