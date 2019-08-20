using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Senders
{
    public interface IPaginatorSenderService : IServiceImplementation
    {
        Task<IUserMessage> SendPaginatorAsync<T>(IMessage msg, string head, IEnumerable<T> data, Func<T, string> displayCallback) where T : class;

        Task<IUserMessage> SendPaginatorAsync<T>(IMessage msg, string head, IEnumerable<T> data, Action<T, EmbedBuilder> displayCallback) where T : class;

        Task<IUserMessage> SendPaginatorRawAsync<T>(IMessage msg, IEnumerable<T> data, Func<T, string> displayCallback) where T : class;

        Task<IUserMessage> SendPlayerPaginatorAsync<T>(IMessage msg, IEnumerable<T> data, Func<T, string> displayCallback) where T : class;
    }
}
