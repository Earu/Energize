namespace Energize.Interfaces.Services
{
    public interface IMinesweeperService : IServiceImplementation
    {
        string Generate(int width, int height, int amount);
    }
}
