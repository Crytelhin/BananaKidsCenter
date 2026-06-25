using Microsoft.AspNetCore.Mvc;
using EntertainmentCenter.API.Services;
using System.Text;

namespace EntertainmentCenter.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly SessionService _sessionService;

        public ReportsController(SessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpGet("daily")]
        public async Task<IActionResult> GetDailyReport([FromQuery] DateTime date, [FromQuery] string lang = "ru", [FromQuery] string period = "day")
        {
            DateTime from, to;
            string fileNamePeriod;
            switch (period.ToLower())
            {
                case "week":
                    // Monday is first day of week
                    var dayOfWeek = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
                    var monday = date.Date.AddDays(-(dayOfWeek - 1));
                    from = monday.ToUniversalTime();
                    to = monday.AddDays(7).AddTicks(-1).ToUniversalTime();
                    fileNamePeriod = $"weekly-{monday:yyyy-MM-dd}";
                    break;
                case "month":
                    var monthStart = new DateTime(date.Year, date.Month, 1);
                    from = monthStart.ToUniversalTime();
                    to = monthStart.AddMonths(1).AddTicks(-1).ToUniversalTime();
                    fileNamePeriod = $"monthly-{date:yyyy-MM}";
                    break;
                case "year":
                    var yearStart = new DateTime(date.Year, 1, 1);
                    from = yearStart.ToUniversalTime();
                    to = yearStart.AddYears(1).AddTicks(-1).ToUniversalTime();
                    fileNamePeriod = $"yearly-{date:yyyy}";
                    break;
                default: // day
                    from = date.Date.ToUniversalTime();
                    to = date.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                    fileNamePeriod = $"daily-{date:yyyy-MM-dd}";
                    break;
            }
            var sessions = await _sessionService.GetHistoryAsync(from, to);

            var t = GetTranslations(lang);
            var csvBuilder = new StringBuilder();

            // Headers — no ID column
            csvBuilder.AppendLine(string.Join(",",
                t.Name, t.Phone, t.Card, t.Zone, t.Tariff, t.Promotion,
                t.Price, t.Created, t.Started, t.Expires, t.Duration, t.Active));

            foreach (var s in sessions)
            {
                var card = s.Client?.CardCode?.StartsWith("nocard") == true
                    ? t.NoCard : s.Client?.CardCode ?? "";
                var promo = s.Promotion?.Name ?? t.None;
                var created = s.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                var activated = s.ActivatedAt.HasValue
                    ? s.ActivatedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                    : t.NotEntered;
                var expires = s.ActivatedAt.HasValue
                    ? s.ExpiresAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                    : t.NotEntered;

                csvBuilder.AppendLine(
                    $"\"{EscapeCsv(s.Client?.FullName)}\"," +
                    $"=\"{EscapeCsv(s.Client?.Phone)}\"," +
                    $"\"{EscapeCsv(card)}\"," +
                    $"\"{EscapeCsv(s.Tariff?.Zone?.Name)}\"," +
                    $"\"{EscapeCsv(s.Tariff?.Label)}\"," +
                    $"\"{EscapeCsv(promo)}\"," +
                    $"{s.FinalPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}," +
                    $"=\"{created}\"," +
                    $"=\"{activated}\"," +
                    $"=\"{expires}\"," +
                    $"{s.DurationMinutes}," +
                    $"\"{(s.IsActive ? t.Yes : t.No)}\""
                );
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csvBuilder.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"report-{fileNamePeriod}.csv");
        }

        private static ReportTranslations GetTranslations(string lang) => lang.ToLower() switch
        {
            "ro" => new ReportTranslations
            {
                Name = "Nume",
                Phone = "Telefon",
                Card = "Card",
                Zone = "Zonă",
                Tariff = "Tarif",
                Promotion = "Promoție",
                Price = "Preț (lei)",
                Created = "Creată",
                Started = "Început",
                Expires = "Expiră",
                Duration = "Durată (min)",
                Active = "Activă",
                NoCard = "Fără card",
                None = "Nimic",
                NotEntered = "Nu a intrat",
                Yes = "Da",
                No = "Nu"
            },
            "en" => new ReportTranslations
            {
                Name = "Name",
                Phone = "Phone",
                Card = "Card",
                Zone = "Zone",
                Tariff = "Tariff",
                Promotion = "Promotion",
                Price = "Price (lei)",
                Created = "Created",
                Started = "Started",
                Expires = "Expires",
                Duration = "Duration (min)",
                Active = "Active",
                NoCard = "No card",
                None = "None",
                NotEntered = "Not entered",
                Yes = "Yes",
                No = "No"
            },
            _ => new ReportTranslations
            {
                Name = "ФИО",
                Phone = "Телефон",
                Card = "Карта",
                Zone = "Зона",
                Tariff = "Тариф",
                Promotion = "Акция",
                Price = "Цена (лей)",
                Created = "Создана",
                Started = "Начало",
                Expires = "Истекает",
                Duration = "Длительность (мин)",
                Active = "Активна",
                NoCard = "Без карты",
                None = "Нет",
                NotEntered = "Не вошёл",
                Yes = "Да",
                No = "Нет"
            }
        };

        private class ReportTranslations
        {
            public string Name { get; init; } = "";
            public string Phone { get; init; } = "";
            public string Card { get; init; } = "";
            public string Zone { get; init; } = "";
            public string Tariff { get; init; } = "";
            public string Promotion { get; init; } = "";
            public string Price { get; init; } = "";
            public string Created { get; init; } = "";
            public string Started { get; init; } = "";
            public string Expires { get; init; } = "";
            public string Duration { get; init; } = "";
            public string Active { get; init; } = "";
            public string NoCard { get; init; } = "";
            public string None { get; init; } = "";
            public string NotEntered { get; init; } = "";
            public string Yes { get; init; } = "";
            public string No { get; init; } = "";
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }
    }
}
