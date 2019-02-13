using Energize.Toolkit;
using System;

namespace Energize.Services.Database
{
    public class DBContext : IDisposable
    {
        private readonly Logger _Log;

        public DBContext(EnergizeDB context, Logger log)
        {
            this.Instance = context;
            this.IsUsed = false;
            this._Log = log;
        }

        public EnergizeDB Instance { get; }

        public bool IsUsed { get; set; }

        public void Dispose()
        {
            try
            {
                this.Instance.SaveChanges(true);
                this.IsUsed = false;
            }
            catch(Exception e)
            {
                this._Log.Nice("Database", ConsoleColor.Red, $"Failed to save changes: {e.Message}");
            }
        }
    }
}
