using Microsoft.AspNetCore.Mvc;
using EntertainmentCenter.API.Models;
using EntertainmentCenter.API.Services;

namespace EntertainmentCenter.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly ClientService _clientService;

        public ClientsController(ClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Client>>> Search([FromQuery] string? q = null)
        {
            var clients = await _clientService.SearchAsync(q);
            return Ok(clients);
        }

        [HttpGet("card/{cardCode}")]
        public async Task<ActionResult<Client>> GetByCard([FromRoute] string cardCode)
        {
            var client = await _clientService.GetByCardCodeAsync(cardCode);
            if (client == null) return NotFound();
            return Ok(client);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Client>> GetById(int id)
        {
            var client = await _clientService.GetByIdAsync(id);
            if (client == null) return NotFound();
            return Ok(client);
        }

        [HttpPost]
        public async Task<ActionResult<Client>> Create([FromBody] Client client)
        {
            try
            {
                var created = await _clientService.AddAsync(client);
                return CreatedAtAction(nameof(GetByCard), new { cardCode = created.CardCode }, created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }
    }
}