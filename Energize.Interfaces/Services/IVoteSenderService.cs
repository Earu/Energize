using Discord;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Interfaces.Services
{
    public interface IVoteSenderService : IServiceImplementation
    {
        Task<(bool, IUserMessage)> SendVote(IMessage msg, string description, IEnumerable<string> choices);
    }
}
