using Energize.Interfaces.Services;

namespace Energize.Services
{
    public class Service : IService
    {
        public Service(string name, IServiceImplementation inst)
        {
            this.Name = name;
            this.Instance = inst;
        }

        public IServiceImplementation Instance { get; private set; }
        public string Name { get; private set; }
    }
}
