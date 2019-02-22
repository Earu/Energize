using System.Collections.Generic;

namespace Energize.Interfaces.Services
{
    public interface ITextStyleService : IServiceImplementation
    {
        string GetStyleResult(string input, string style);

        List<string> GetStyles();
    }
}
