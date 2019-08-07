using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Development
{
    public interface ICSharpEvaluationService : IServiceImplementation
    {
        Task<(int, string)> Eval(string code, object ctx);
    }
}
