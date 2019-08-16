using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Development
{
    public interface IMailingService : IServiceImplementation
    {
        Task SendMailAsync(string toAddress, string subject, string body);
    }
}
