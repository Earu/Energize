using Energize.Web.Models;
using Energize.Web.Services;
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
        private OctoClient Client;

        // GET: api/commands
        [HttpGet]
        public async Task<CommandInformation> Get()
        {
            CommandInformation cmdInfo = null;
            try
            {
                this.Client = TransmissionService.Instance.Client;
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
