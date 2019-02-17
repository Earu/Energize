namespace Energize.ServiceInterfaces
{
    public interface IServiceManager
    {
        T GetService<T>(string name) where T : IServiceImplementation;
    }
}
