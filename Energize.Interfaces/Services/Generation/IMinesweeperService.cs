namespace Energize.Interfaces.Services.Generation
{
    public interface IMinesweeperService : IServiceImplementation
    {
        string Generate(int width, int height, int amount);
    }
}
