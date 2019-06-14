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
        // GET: api/commands
        [HttpGet]
        public async Task<CommandInformation> Get()
        {
            CommandInformation cmdInfo = null;
            try
            {
                OctoClient client = TransmissionService.Instance.Client;
                if (!client.IsConnected)
                    await client.ConnectAsync();

                if (client.TryGetProcess("Energize", out RemoteProcess proc))
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
