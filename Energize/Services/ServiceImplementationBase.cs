﻿using System.Threading.Tasks;
using Energize.Interfaces.Services;

namespace Energize.Services
{
    public abstract class ServiceImplementationBase : IServiceImplementation
    {
        public virtual void Initialize() { }

        public virtual Task InitializeAsync()
            => Task.CompletedTask;
    }
}
