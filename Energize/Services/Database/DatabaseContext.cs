﻿using Energize.Interfaces.Services.Database;
using Energize.Essentials;
using System;

namespace Energize.Services.Database
{
    public class DatabaseContext : IDatabaseContext
    {
        private readonly Logger _Log;

        public DatabaseContext(Database context, Logger log)
        {
            this.Instance = context;
            this.IsUsed = false;
            this._Log = log;
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
            catch(Exception e)
            {
                this._Log.Nice("Database", ConsoleColor.Red, $"Failed to save changes: {e.Message}");
            }
        }
    }
}