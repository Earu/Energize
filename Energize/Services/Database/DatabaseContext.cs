using Energize.Interfaces.Services.Database;
using Energize.Essentials;
using System;

namespace Energize.Services.Database
{
    public class DatabaseContext : IDatabaseContext
    {
        private readonly Logger Logger;

        public DatabaseContext(Database context, Logger logger)
        {
            this.Instance = context;
            this.IsUsed = false;
            this.Logger = logger;
        }

        public IDatabase Instance { get; }

        public bool IsUsed { get; set; }

        public void Dispose()
        {
            try
            {
                this.Instance.Save();
                this.IsUsed = false;
            }
            catch(Exception ex)
            {
                this.Logger.Nice("Database", ConsoleColor.Red, $"Failed to save changes: {ex.Message}");
            }
        }
    }
}
