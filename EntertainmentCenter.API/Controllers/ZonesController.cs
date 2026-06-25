using Microsoft.AspNetCore.Mvc;
using EntertainmentCenter.API.Models;
using EntertainmentCenter.API.Services;

namespace EntertainmentCenter.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ZonesController : ControllerBase
    {
        private readonly ZoneService _zoneService;
        private readonly TariffService _tariffService;

        public ZonesController(ZoneService zoneService, TariffService tariffService)
        {
            _zoneService = zoneService;
            _tariffService = tariffService;
        }

        // GET api/zones
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Zone>>> GetAllZones([FromQuery] bool all = false)
        {
            var zones = await _zoneService.GetAllWithTariffsAsync(all);
            return Ok(zones);
        }

        // GET api/zones/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Zone>> GetById(int id)
        {
            var zone = await _zoneService.GetByIdAsync(id);
            if (zone == null) return NotFound();
            return Ok(zone);
        }

        // POST api/zones
        [HttpPost]
        public async Task<ActionResult<Zone>> CreateZone([FromBody] Zone zone)
        {
            var createdZone = await _zoneService.SaveZoneAsync(zone);
            return CreatedAtAction(nameof(GetById), new { id = createdZone.Id }, createdZone);
        }

        // PUT api/zones/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateZone(int id, [FromBody] Zone zone)
        {
            if (id != zone.Id) return BadRequest();
            var updated = await _zoneService.SaveZoneAsync(zone);
            return Ok(updated);
        }

        // DELETE api/zones/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteZone(int id)
        {
            var existing = await _zoneService.GetByIdAsync(id);
            if (existing == null) return NotFound();
            await _zoneService.DeleteZoneAsync(id);
            return NoContent();
        }

        // POST api/zones/{id}/tariffs
        [HttpPost("{id:int}/tariffs")]
        public async Task<ActionResult<Tariff>> AddTariff(int id, [FromBody] Tariff tariff)
        {
            var zone = await _zoneService.GetByIdAsync(id);
            if (zone == null) return NotFound();

            tariff.ZoneId = id;
            var created = await _tariffService.SaveTariffAsync(tariff);
            return CreatedAtAction(nameof(GetById), new { id }, created);
        }

        // DELETE api/tariffs/{id}
        [HttpDelete("/api/tariffs/{id:int}")]
        public async Task<IActionResult> DeleteTariff(int id)
        {
            await _tariffService.DeleteTariffAsync(id);
            return NoContent();
        }
    }
}