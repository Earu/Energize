using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Interfaces.Services
{
    public interface IPaginatorSenderService : IServiceImplementation
    {
        Task<IUserMessage> SendPaginator<T>(IMessage msg, string head, IEnumerable<T> data, Func<T, string> displaycallback) where T : class;

        Task<IUserMessage> SendPaginator<T>(IMessage msg, string head, IEnumerable<T> data, Action<T, EmbedBuilder> displaycallback) where T : class;

        Task<IUserMessage> SendPaginatorRaw<T>(IMessage msg, IEnumerable<T> data, Func<T, string> displaycallback) where T : class;
    }
}
