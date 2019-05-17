using Energize.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Octovisor.Client;
using Octovisor.Client.Exceptions;
using System.Threading.Tasks;

namespace Energize.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommandsController : ControllerBase
    {
        private readonly OctoClient Client;

        public CommandsController()
        {
            OctoConfig config = OctoConfig.FromFile("octo_config.yaml");
            this.Client = new OctoClient(config);
        }

        // GET: api/commands
        [HttpGet]
        public async Task<CommandInformation> Get()
        {
            CommandInformation cmdInfo = null;
            try
            {
                if (!this.Client.IsConnected)
                    await this.Client.ConnectAsync();

                if (this.Client.TryGetProcess("Energize", out RemoteProcess proc))
                    cmdInfo = await proc.TransmitAsync<CommandInformation>("commands");

                return cmdInfo ?? new CommandInformation();
            }
            catch(TimeOutException)
            {
                return new CommandInformation();
            }
        }
    }
}
