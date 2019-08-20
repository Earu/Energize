using Discord;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Senders
{
    public interface IVoteSenderService : IServiceImplementation
    {
        Task<IUserMessage> SendVoteAsync(IMessage msg, string description, IEnumerable<string> choices);
    }
}
