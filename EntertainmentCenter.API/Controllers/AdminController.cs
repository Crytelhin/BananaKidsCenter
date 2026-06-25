using Microsoft.AspNetCore.Mvc;
using EntertainmentCenter.API.Services;

namespace EntertainmentCenter.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;
        private readonly SessionService _sessionService;

        public AdminController(AdminService adminService, SessionService sessionService)
        {
            _adminService = adminService;
            _sessionService = sessionService;
        }

        [HttpPost("verify-pin")]
        public async Task<ActionResult<bool>> VerifyPin([FromBody] VerifyPinRequest request)
        {
            var result = await _adminService.VerifyPinAsync(request.Pin);
            return Ok(result);
        }

        [HttpPost("change-pin")]
        public async Task<ActionResult<bool>> ChangePin([FromBody] ChangePinRequest request)
        {
            var result = await _adminService.ChangePinAsync(request.NewPin);
            return Ok(result);
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardMetrics>> GetDashboard()
        {
            var metrics = await _sessionService.GetDashboardMetricsAsync();
            return Ok(metrics);
        }

        [HttpGet("notification-settings")]
        public async Task<ActionResult<NotificationSettingsResponse>> GetNotificationSettings()
        {
            var config = await _adminService.GetConfigAsync();
            return Ok(new NotificationSettingsResponse
            {
                WarningEnabled = config.WarningEnabled,
                WarningMinutesBeforeExpiry = config.WarningMinutesBeforeExpiry
            });
        }

        [HttpPut("notification-settings")]
        public async Task<IActionResult> UpdateNotificationSettings([FromBody] UpdateNotificationSettingsRequest request)
        {
            var result = await _adminService.UpdateNotificationSettingsAsync(request.WarningEnabled, request.WarningMinutesBeforeExpiry);
            if (!result) return NotFound();
            return NoContent();
        }
    }

    public class VerifyPinRequest
    {
        public string Pin { get; set; } = "";
    }

    public class ChangePinRequest
    {
        public string NewPin { get; set; } = "";
    }

    public class NotificationSettingsResponse
    {
        public bool WarningEnabled { get; set; }
        public int WarningMinutesBeforeExpiry { get; set; }
    }

    public class UpdateNotificationSettingsRequest
    {
        public bool WarningEnabled { get; set; }
        public int WarningMinutesBeforeExpiry { get; set; }
    }
}