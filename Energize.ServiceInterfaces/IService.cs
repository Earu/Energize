namespace Energize.ServiceInterfaces
{
    public interface IService
    {
        IServiceImplementation Instance { get; }
        string Name { get; }
    }
}
