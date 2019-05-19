using Energize.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Octovisor.Client;
using Octovisor.Messages;
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
            this.Client.Log += Client_Log;
        }

        private void Client_Log(LogMessage obj)
        {
            System.IO.File.AppendAllText("log.log", obj.Content + "\n");
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
