using System;
using System.Collections.Generic;
using System.Text;

namespace Energize.Services.Database
{
    public class DBContext : IDisposable
    {
        private EnergizeDB _Context;
        private bool _IsUsed;
        private EnergizeLog _Log;

        public DBContext(EnergizeDB context,EnergizeLog log)
        {
            this._Context = context;
            this._IsUsed = false;
            this._Log = log;
        }

        public EnergizeDB Context { get => this._Context; }
        public bool IsUsed { get => this._IsUsed; }

        public void Dispose()
        {
            try
            {
                this.Context.SaveChanges(true);
                this._IsUsed = false;
            }
            catch(Exception e)
            {
                this._Log.Nice("Database", ConsoleColor.Red, $"Failed to save changes: {e.Message}");
            }
        }
    }
}
