using System;

namespace Energize.Interfaces.Services.Database
{
    public interface IDatabaseContext : IDisposable
    {
        IDatabase Instance { get; }

        bool IsUsed { get; set; }
    }
}
