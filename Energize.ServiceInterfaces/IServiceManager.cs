namespace Energize.Interfaces
{
    public interface IServiceManager
    {
        T GetService<T>(string name) where T : IServiceImplementation;
    }
}
