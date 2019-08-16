using Energize.Essentials;
using Octovisor.Client;

namespace Energize.Services.Transmission.Transmitters
{
    internal class BaseTransmitter
    {
        protected OctoClient Client;
        protected Logger Logger;

        internal BaseTransmitter(EnergizeClient client, OctoClient octoClient)
        {
            this.Client = octoClient;
            this.Logger = client.Logger;
        }

        internal virtual void Initialize() { }

        protected void Log(string message)
            => this.Logger.LogTo("ipc.log", message);
    }
}
