using Microsoft.AspNetCore.Mvc;
using EntertainmentCenter.API.Models;
using EntertainmentCenter.API.Services;

namespace EntertainmentCenter.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionsController : ControllerBase
    {
        private readonly SessionService _sessionService;

        public SessionsController(SessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<Session>>> GetActive()
        {
            var sessions = await _sessionService.GetAllActiveAsync();
            return Ok(sessions);
        }

        /// <summary>
        /// Проверяет карточку на контроле.
        /// При первом сканировании — активирует сессию и запускает таймер.
        /// При повторных — возвращает оставшееся время.
        /// </summary>
        [HttpGet("check/{cardCode}")]
        public async Task<ActionResult<Session>> CheckEntry([FromRoute] string cardCode)
        {
            // 1. Ищем уже активную (не истекшую) сессию
            var session = await _sessionService.GetActiveByCardCodeAsync(cardCode);
            if (session != null) return Ok(session);

            // 2. Ищем сессию в статусе "ожидает входа" → активируем (первое сканирование)
            var activated = await _sessionService.ActivateSessionAsync(cardCode);
            if (activated != null) return Ok(activated);

            // 3. Ничего не найдено → отказ
            return NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<Session>> Start([FromBody] StartSessionRequest request)
        {
            var session = await _sessionService.StartSessionAsync(
                request.ClientId, request.TariffId, request.PromotionId, request.CustomDurationMinutes, request.ActivateImmediately);
            return CreatedAtAction(nameof(CheckEntry), new { cardCode = session.Client?.CardCode ?? "" }, session);
        }

        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<Session>>> GetHistory([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var sessions = await _sessionService.GetHistoryAsync(from, to);
            return Ok(sessions);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Session>> GetById(int id)
        {
            var session = await _sessionService.GetByIdAsync(id);
            if (session == null) return NotFound();
            return Ok(session);
        }

        [HttpPost("{id:int}/end")]
        public async Task<IActionResult> EndSession(int id)
        {
            var result = await _sessionService.EndSessionAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }

        [HttpPost("{id:int}/extend")]
        public async Task<ActionResult<Session>> ExtendSession(int id, [FromBody] ExtendSessionRequest request)
        {
            var session = await _sessionService.ExtendSessionAsync(
                id, request.TariffId, request.PromotionId, request.CustomDurationMinutes);
            if (session == null) return NotFound();
            return Ok(session);
        }
    }

    public class StartSessionRequest
    {
        public int ClientId { get; set; }
        public int TariffId { get; set; }
        public int? PromotionId { get; set; }
        public int? CustomDurationMinutes { get; set; }
        public bool ActivateImmediately { get; set; }
    }

    public class ExtendSessionRequest
    {
        public int TariffId { get; set; }
        public int? PromotionId { get; set; }
        public int? CustomDurationMinutes { get; set; }
    }
}