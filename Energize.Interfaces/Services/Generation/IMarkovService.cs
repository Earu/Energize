namespace Energize.Interfaces.Services.Generation
{
    public interface IMarkovService : IServiceImplementation
    {
        string Generate(string data);
    }
}
