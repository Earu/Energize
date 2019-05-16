using Energize.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Octovisor.Client;
using System.Collections.Generic;
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
        public async Task<IEnumerable<Command>> Get()
        {
            if (!this.Client.IsConnected)
                await this.Client.ConnectAsync();

            List<Command> cmds = null;
            if (this.Client.TryGetProcess("Energize", out RemoteProcess proc))
                cmds = await proc.TransmitAsync<List<Command>>("commands");

            return cmds;
        }
    }
}
