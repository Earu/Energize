using System;
using System.Collections.Generic;
using System.Text;

namespace Energize.Services.Database
{
    public class DBContext : IDisposable
    {
        private EnergizeDB _Instance;
        private bool _IsUsed;
        private EnergizeLog _Log;

        public DBContext(EnergizeDB context,EnergizeLog log)
        {
            this._Instance = context;
            this._IsUsed = false;
            this._Log = log;
        }

        public EnergizeDB Instance { get => this._Instance; }
        public bool IsUsed { get => this._IsUsed; set => this._IsUsed = value; }

        public void Dispose()
        {
            try
            {
                this._Instance.SaveChanges(true);
                this._IsUsed = false;
            }
            catch(Exception e)
            {
                this._Log.Nice("Database", ConsoleColor.Red, $"Failed to save changes: {e.Message}");
            }
        }
    }
}
