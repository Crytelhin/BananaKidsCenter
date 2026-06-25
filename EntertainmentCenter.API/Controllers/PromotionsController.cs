using Microsoft.AspNetCore.Mvc;
using EntertainmentCenter.API.Models;
using EntertainmentCenter.API.Services;

namespace EntertainmentCenter.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromotionsController : ControllerBase
    {
        private readonly PromotionService _promotionService;

        public PromotionsController(PromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Promotion>>> GetActive([FromQuery] bool all = false)
        {
            var promotions = all
                ? await _promotionService.GetAllAsync()
                : await _promotionService.GetActiveAsync();
            return Ok(promotions);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Promotion>> GetById(int id)
        {
            var promotion = await _promotionService.GetByIdAsync(id);
            if (promotion == null) return NotFound();
            return Ok(promotion);
        }

        [HttpPost]
        public async Task<ActionResult<Promotion>> Create([FromBody] Promotion promotion)
        {
            var created = await _promotionService.SavePromotionAsync(promotion);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Promotion promotion)
        {
            if (id != promotion.Id) return BadRequest();
            var updated = await _promotionService.SavePromotionAsync(promotion);
            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _promotionService.GetByIdAsync(id);
            if (existing == null) return NotFound();
            await _promotionService.DeletePromotionAsync(id);
            return NoContent();
        }
    }
}