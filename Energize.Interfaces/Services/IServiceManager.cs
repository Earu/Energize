namespace Energize.Interfaces.Services
{
    public interface IServiceManager
    {
        T GetService<T>(string name) where T : IServiceImplementation;
    }
}
