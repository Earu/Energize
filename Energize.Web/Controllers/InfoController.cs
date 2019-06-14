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
    public class InfoController : ControllerBase
    {
        // GET: api/info
        [HttpGet]
        public async Task<BotInformation> Get()
        {
            BotInformation botInfo = null;
            try
            {
                OctoClient client = TransmissionService.Instance.Client;
                if (!client.IsConnected)
                    await client.ConnectAsync();

                if (client.TryGetProcess("Energize", out RemoteProcess proc))
                    botInfo = await proc.TransmitAsync<BotInformation>("info");

                return botInfo ?? new BotInformation();
            }
            catch (TimeOutException)
            {
                return new BotInformation();
            }
        }
    }
}
