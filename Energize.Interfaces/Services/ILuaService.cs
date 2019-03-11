using Discord;
using Energize.Toolkit;
using System.Collections.Generic;

namespace Energize.Interfaces.Services
{
    public interface ILuaService : IServiceImplementation
    {
        bool Run(IMessage msg, string code, out List<object> returns, out string error, Logger log);

        void Reset(ulong chanid, Logger log);
    }
}
