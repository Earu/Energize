using System;

namespace Energize.Interfaces.Services
{
    public interface IDatabaseContext : IDisposable
    {
        IDatabase Instance { get; }

        bool IsUsed { get; set; }
    }
}
