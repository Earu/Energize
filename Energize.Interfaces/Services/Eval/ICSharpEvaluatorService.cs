using System.Threading.Tasks;

namespace Energize.Interfaces.Services.Eval
{
    public interface ICSharpEvaluatorService : IServiceImplementation
    {
        Task<(int, string)> Eval(string code, object ctx);
    }
}
