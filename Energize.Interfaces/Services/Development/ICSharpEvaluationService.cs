using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Development
{
    public interface ICSharpEvaluationService : IServiceImplementation
    {
        Task<(int, string)> EvalAsync(string code, object ctx);
    }
}
