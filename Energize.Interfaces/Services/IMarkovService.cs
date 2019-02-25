namespace Energize.Interfaces.Services
{
    public interface IMarkovService : IServiceImplementation
    {
        string Generate(string data);
    }
}
