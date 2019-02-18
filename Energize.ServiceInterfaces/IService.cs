namespace Energize.Interfaces
{
    public interface IService
    {
        IServiceImplementation Instance { get; }
        string Name { get; }
    }
}
