namespace Energize.Interfaces.Services
{
    public interface IService
    {
        IServiceImplementation Instance { get; }
        string Name { get; }
    }
}
